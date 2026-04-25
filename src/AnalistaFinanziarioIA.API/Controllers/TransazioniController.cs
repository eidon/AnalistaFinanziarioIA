using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TransazioniController(
    IPortafoglioRepository _repository,
    ITransazioneService _transazioneService) : ControllerBase
{
    // Recupera la storia per la sezione inferiore
    [HttpGet("utente/{utenteId}")]
    public async Task<IActionResult> GetStoria(Guid utenteId, [FromQuery] string? search)
    {
        var storia = await _repository.GetStoriaFiltrataAsync(utenteId, search);

        var risultato = storia.Select(t => new {
            t.Id,
            t.Data,
            t.Tipo,
            t.Quantita,
            t.PrezzoUnitario,
            t.Note,
            TitoloNome = t.AssetPortafoglio?.Titolo?.Nome ?? "N/A",
            Ticker = t.AssetPortafoglio?.Titolo?.Simbolo ?? "N/A"
        });

        return Ok(risultato);
    }

    // ELIMINA: Fondamentale per correggere errori
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var successo = await _repository.EliminaTransazioneAsync(id);
        if (!successo) return NotFound();
        return NoContent();
    }

    // MODIFICA: Per cambiare prezzo o quantità senza cancellare
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RegistraTransazioneDto dto)
    {
        var transazioneUpdate = new Transazione
        {
            Quantita = dto.Quantita,
            PrezzoUnitario = dto.PrezzoUnitario,
            Commissioni = dto.Commissioni,
            Tasse = dto.Tasse,
            Note = dto.Note ?? "",
            Data = dto.Data
        };

        var successo = await _repository.AggiornaTransazioneAsync(id, transazioneUpdate);

        if (!successo) return NotFound();
        return Ok(new { message = "Transazione aggiornata con successo" });
    }

    [HttpPost("registra")]
    public async Task<IActionResult> Registra([FromBody] RegistraTransazioneDto dto)
    {
        if (dto == null || dto.TitoloLookup == null)
            return BadRequest("Dati della transazione o del titolo mancanti.");

        try
        {
            var successo = await _transazioneService.RegistraOperazioneAsync(dto);

            if (successo)
                return Ok(new { message = "Transazione registrata con successo" });

            return StatusCode(500, "Errore durante il salvataggio.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { errore = ex.Message });
        }
    }

    // Recupera una singola transazione per la modifica
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var transazione = await _repository.GetTransazioneByIdAsync(id);

        if (transazione == null) return NotFound();

        return Ok(new
        {
            transazione.Id,
            transazione.Data,
            transazione.Tipo,
            transazione.Quantita,
            transazione.PrezzoUnitario,
            transazione.Commissioni,
            transazione.Tasse,
            transazione.Note,
            AssetPortafoglio = new
            {
                Titolo = new
                {
                    Nome = transazione.AssetPortafoglio?.Titolo?.Nome ?? "N/A",
                    Isin = transazione.AssetPortafoglio?.Titolo?.Isin ?? "N/A"
                }
            }
        });
    }

}
