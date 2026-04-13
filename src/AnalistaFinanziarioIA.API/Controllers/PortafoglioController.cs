using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces; // Dove sta 'IPortafoglioRepository'
using AnalistaFinanziarioIA.Core.Models;     // Dove sta 'Transazione'
using AnalistaFinanziarioIA.Core.Services;   // Dove sta 'PortafoglioService'
using AnalistaFinanziarioIA.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;


namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortafoglioController(
    IPortafoglioRepository _repository,
    ITitoloRepository _titoloRepository,
    ITitoloService _titoloService,
    IValidator<TransazioneInputDto> _validator,
    PortafoglioService _portafoglioService) : ControllerBase
{

    /// <summary>
    /// Recupera la dashboard calcolata con totali convertiti e asset.
    /// </summary>
    [HttpGet("dashboard/{utenteId}")]
    public async Task<IActionResult> GetDashboard(Guid utenteId)
    {
        // 2. USIAMO IL SERVICE (che gestisce tassi di cambio e nomi corretti)
        // Questo risolverà il problema del 404 e dei totali a 0
        var dashboard = await _portafoglioService.GetDashboardCompletaAsync(utenteId);
        return Ok(dashboard);
    }

    [HttpPost("transazione")]
    public async Task<IActionResult> RegistraOperazione([FromBody] TransazioneInputDto dto)
    {
        // 1. Validazione (FluentValidation)
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            // 2. Determiniamo l'ID del titolo (esistente o nuovo)
            int idTitoloEffettivo;

            if (dto.TitoloId.HasValue && dto.TitoloId.Value > 0)
            {
                idTitoloEffettivo = dto.TitoloId.Value;
            }
            else if (dto.TitoloLookup != null)
            {
                // Il titolo non ha ID, viene da Alpha Vantage. 
                // Verifichiamo se esiste già per simbolo (magari aggiunto da un altro utente)
                var titoloEsistente = await _titoloRepository.GetBySimboloAsync(dto.TitoloLookup.Simbolo);

                if (titoloEsistente != null)
                {
                    idTitoloEffettivo = titoloEsistente.Id;
                }
                else
                {
                    TipoTitolo categoria = _titoloService.MappaTipoTitolo(dto.TitoloLookup.Tipo);

                    // Dobbiamo crearlo (Censimento)
                    var nuovoTitolo = new Titolo
                    {
                        Simbolo = dto.TitoloLookup.Simbolo,
                        Nome = dto.TitoloLookup.Nome,
                        Isin = dto.TitoloLookup.Isin,
                        Valuta = dto.TitoloLookup.Valuta ?? "EUR", // Uso EUR come fallback
                        DataCreazione = DateTime.Now,
                        DataUltimoPrezzo = DateTime.Now,
                        Mercato = dto.TitoloLookup.Regione
                    };

                    nuovoTitolo.Categoria = dto.TitoloLookup.Tipo?.ToLower().Contains("etf") == true
                        ? TipoTitolo.ETF
                        : TipoTitolo.Azione;

                    // AddAsync deve salvare e restituire l'ID generato
                    await _titoloRepository.AddAsync(nuovoTitolo);
                    idTitoloEffettivo = nuovoTitolo.Id;
                }
            }
            else
            {
                return BadRequest(new { Errore = "Nessun titolo selezionato correttamente." });
            }

            // 3. Creiamo l'entità Transazione usando i dati del DTO
            var nuovaTransazione = new Transazione
            {
                Quantita = dto.Quantita,
                PrezzoUnita = dto.PrezzoUnita,
                Commissioni = dto.Commissioni,
                Tasse = dto.Tasse,
                Note = dto.Note,
                Data = dto.Data ?? DateTime.UtcNow,
                Tipo = TipoTransazione.Acquisto
                // Nota: UtenteId e TitoloId vengono gestiti dal repository sotto
            };

            // 4. Salvataggio finale
            // Passiamo dto.UtenteId (che ora arriva nel Body) e idTitoloEffettivo (calcolato sopra)
            var assetAggiornato = await _repository.AggiungiTransazioneAsync(nuovaTransazione, dto.UtenteId, idTitoloEffettivo);

            return Ok(new
            {
                Messaggio = "Operazione registrata con successo",
                TitoloId = idTitoloEffettivo,
                NuovaQuantita = assetAggiornato.QuantitaTotale,
                NuovoPrezzoMedio = assetAggiornato.PrezzoMedioCarico
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Errore = ex.Message });
        }
    }

    [HttpGet("utente/{utenteId}")]
    public async Task<ActionResult<IEnumerable<StoriaTransazioniDto>>> GetStoria(Guid utenteId, [FromQuery] string? search)
    {
        var transazioni = await _repository.GetStoriaFiltrataAsync(utenteId, search);

        var risultato = transazioni
            .GroupBy(t => new { t.Data.Year, t.Data.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g => new StoriaTransazioniDto
            {
                Periodo = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                Movimenti = g.Select(t => new MovimentoDto
                {
                    Id = t.Id,
                    GiornoMese = t.Data.ToString("dd.MM"),
                    TitoloNome = t.AssetPortafoglio?.Titolo?.Nome ?? "N/A",
                    Ticker = t.AssetPortafoglio?.Titolo?.Simbolo ?? "",
                    Tipo = t.Tipo.ToString(),
                    Descrizione = $"{(t.Tipo == TipoTransazione.Acquisto ? "Acquistato" : "Venduto")} x{t.Quantita:N0} a {t.PrezzoUnita:N2} {t.AssetPortafoglio?.Titolo?.Valuta}",
                    TotaleOperazione = t.Quantita * t.PrezzoUnita,
                    Valuta = t.AssetPortafoglio?.Titolo?.Valuta ?? "EUR",
                    Icona = t.Tipo == TipoTransazione.Acquisto ? "in" : "out"
                }).ToList()
            }).ToList();

        return Ok(risultato);
    }
}