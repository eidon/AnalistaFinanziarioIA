using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortafoglioController : ControllerBase
{

    private readonly IPortafoglioRepository _repository;
    private readonly IValidator<TransazioneInputDto> _validator; // Iniezione del validatore

    public PortafoglioController(IPortafoglioRepository repository, IValidator<TransazioneInputDto> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    /// <summary>
    /// Registra un nuovo acquisto o vendita nel portafoglio.
    /// </summary>
    [HttpPost("transazione")]
    public async Task<IActionResult> RegistraOperazione(
        [FromQuery] Guid utenteId,
        [FromQuery] int titoloId,
        [FromBody] TransazioneInputDto dto)
    {
        // 1. Validazione manuale ed esplicita
        var validationResult = await _validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            // Restituisce un 400 con i dettagli degli errori (es: "La quantità deve essere > 0")
            return BadRequest(validationResult.Errors);
        }

        try
        {
            // 2. Mapping DTO -> Entità
            // Il server decide i campi sensibili (UtenteId, TitoloId, Data se manca)
            var nuovaTransazione = new Transazione
            {
                Quantita = dto.Quantita,
                PrezzoUnita = dto.PrezzoUnita,
                Commissioni = dto.Commissioni,
                Tasse = dto.Tasse,
                Note = dto.Note,
                Data = dto.Data ?? DateTime.UtcNow,
                Tipo = TipoTransazione.Acquisto // Puoi anche passarlo nel DTO se vuoi gestire vendite
            };

            // 2. Passiamo l'entità pulita al repository
            var assetAggiornato = await _repository.AggiungiTransazioneAsync(nuovaTransazione, utenteId, titoloId);

            return Ok(new
            {
                Messaggio = "Operazione registrata con successo",
                // 3. Usiamo l'ID restituito dall'asset aggiornato o il parametro titoloId
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



    /// <summary>
    /// Recupera la situazione attuale del portafoglio di un utente.
    /// </summary>
    [HttpGet("{utenteId}")]
    public async Task<IActionResult> GetPortafoglio(Guid utenteId)
    {
        try
        {
            // 1. Recuperiamo gli asset con tutti i dati necessari
            var assets = await _repository.GetPortafoglioUtenteAsync(utenteId);

            if (assets == null) return Ok(new List<object>());

            // 2. Mappatura manuale per evitare loop circolari e crash
            var listaDto = assets.Select(asset => {

                // Se il prezzo nel DB è 0, usa il PMC così il rendimento sarà 0 invece di -100%
                decimal prezzoAttuale = asset.Titolo?.UltimoPrezzo > 0
                        ? asset.Titolo.UltimoPrezzo
                        : asset.PrezzoMedioCarico;

                // Calcolo manuale rapido qui per evitare chiamate async multiple nel loop
                // che spesso causano l'errore 500 se il context DB non è thread-safe
                decimal dividendiNetti = asset.Dividendi?.Sum(d => d.ImportoLordo - d.TasseTrattenute) ?? 0;
                decimal valoreAttualeMercato = asset.QuantitaTotale * prezzoAttuale;
                decimal costoTotaleAcquisto = asset.QuantitaTotale * asset.PrezzoMedioCarico;

                decimal rendimentoTotale = (valoreAttualeMercato + dividendiNetti) - costoTotaleAcquisto;

                return new
                {
                    Id = asset.Id,
                    QuantitaTotale = asset.QuantitaTotale,
                    PrezzoMedioCarico = asset.PrezzoMedioCarico,
                    RendimentoTotale = rendimentoTotale,
                    Titolo = new
                    {
                        Simbolo = asset.Titolo?.Simbolo ?? "N/A",
                        Nome = asset.Titolo?.Nome ?? "Sconosciuto",
                        Valuta = asset.Titolo?.Valuta ?? "EUR",
                        UltimoPrezzo = prezzoAttuale
                    }
                };
            }).ToList();

            return Ok(listaDto);
        }
        catch (Exception ex)
        {
            // Questo ti permette di vedere l'errore vero nei log invece di un generico 500
            return StatusCode(500, $"Errore interno: {ex.Message}");
        }
    }
}