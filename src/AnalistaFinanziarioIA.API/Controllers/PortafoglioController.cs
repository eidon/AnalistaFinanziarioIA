using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using AnalistaFinanziarioIA.Core.Models;     // Dove sta 'Transazione'
using AnalistaFinanziarioIA.Core.Interfaces; // Dove sta 'IPortafoglioRepository'
using AnalistaFinanziarioIA.Core.Services;   // Dove sta 'PortafoglioService'
using AnalistaFinanziarioIA.Core.DTOs;


namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// 1. ABBIAMO AGGIUNTO PortafoglioService NEL COSTRUTTORE
public class PortafoglioController(
    IPortafoglioRepository _repository,
    IValidator<TransazioneInputDto> _validator,
    IConfiguration _configuration,
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
    public async Task<IActionResult> RegistraOperazione(
        [FromQuery] Guid utenteId,
        [FromQuery] int titoloId,
        [FromBody] TransazioneInputDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        try
        {
            var nuovaTransazione = new Transazione
            {
                Quantita = dto.Quantita,
                PrezzoUnita = dto.PrezzoUnita,
                Commissioni = dto.Commissioni,
                Tasse = dto.Tasse,
                Note = dto.Note,
                Data = dto.Data ?? DateTime.UtcNow,
                Tipo = TipoTransazione.Acquisto
            };

            var assetAggiornato = await _repository.AggiungiTransazioneAsync(nuovaTransazione, utenteId, titoloId);

            return Ok(new
            {
                Messaggio = "Operazione registrata con successo",
                TitoloId = titoloId,
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