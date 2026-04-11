using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortafoglioController(
    IPortafoglioRepository _repository,
    IValidator<TransazioneInputDto> _validator,
    IConfiguration _configuration) : ControllerBase
{

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
            var assets = await _repository.GetPortafoglioUtenteAsync(utenteId);
            if (assets == null) return Ok(new List<object>());

            // 1. Recuperiamo il tasso di cambio REALE una volta sola prima del ciclo
            decimal tassoUsdEur = 0.92m; // Fallback di sicurezza
            try
            {
                string apiKey = _configuration["ExchangeRate:ApiKey"];
                using HttpClient client = new HttpClient();
                var response = await client.GetFromJsonAsync<JsonElement>($"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD");
                tassoUsdEur = response.GetProperty("conversion_rates").GetProperty("EUR").GetDecimal();
            }
            catch (Exception)
            {
                // Se l'API fallisce, userà il valore di 0.92 impostato sopra
            }

            var listaDto = new List<object>();

            foreach (var asset in assets)
            {
                decimal prezzoAttuale = asset.Titolo?.UltimoPrezzo > 0
                        ? asset.Titolo.UltimoPrezzo
                        : asset.PrezzoMedioCarico;

                decimal rendimentoTotaleRaw = await _repository.CalcolaRendimentoTotaleAsync(asset.Id, prezzoAttuale);

                // 2. Usiamo il tasso dinamico recuperato dall'API
                decimal rendimentoInEur = asset.Titolo?.Valuta == "USD"
                    ? rendimentoTotaleRaw * tassoUsdEur  // Se l'API dice 0.92, 100$ diventano 92€
                    : rendimentoTotaleRaw;

                listaDto.Add(new
                {
                    Id = asset.Id,
                    QuantitaTotale = asset.QuantitaTotale,
                    PrezzoMedioCarico = asset.PrezzoMedioCarico,
                    RendimentoTotale = rendimentoInEur,
                    Titolo = new
                    {
                        Simbolo = asset.Titolo?.Simbolo ?? "N/A",
                        Nome = asset.Titolo?.Nome ?? "Sconosciuto",
                        Valuta = asset.Titolo?.Valuta ?? "EUR",
                        UltimoPrezzo = prezzoAttuale
                    }
                });
            }

            return Ok(listaDto);
        }
        catch (Exception ex)
        {
            // Questo ti permette di vedere l'errore vero nei log invece di un generico 500
            return StatusCode(500, $"Errore interno: {ex.Message}");
        }
    }

    [HttpGet("storia/{utenteId}")]
    public async Task<IActionResult> GetStoria(Guid utenteId, [FromQuery] string? search)
    {
        var transazioni = await _repository.GetStoriaFiltrataAsync(utenteId, search);

        // Trasformiamo i dati per la UI "a timeline"
        var risultato = transazioni
            .GroupBy(t => new { t.Data.Year, t.Data.Month })
            .Select(g => new
            {
                MeseAnno = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                Transazioni = g.Select(t => new
                {
                    t.Id,
                    t.Data,
                    GiornoMese = t.Data.ToString("dd.MM"),
                    Titolo = t.AssetPortafoglio.Titolo.Nome,
                    Ticker = t.AssetPortafoglio.Titolo.Simbolo,
                    Tipo = t.Tipo.ToString(),
                    Dettaglio = $"{t.Tipo} x{t.Quantita} a {t.PrezzoUnita:N2} {t.AssetPortafoglio.Titolo.Valuta}",
                    Totale = t.Quantita * t.PrezzoUnita,
                    Valuta = t.AssetPortafoglio.Titolo.Valuta
                })
            })
            .ToList();

        return Ok(risultato);
    }
}