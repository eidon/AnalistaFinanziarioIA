using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class TransazioniController(IPortafoglioRepository _repository) : ControllerBase
{
    // Recupera la storia per la sezione inferiore
    [HttpGet("utente/{utenteId}")]
    public async Task<IActionResult> GetStoria(Guid utenteId, [FromQuery] string? search)
    {
        // Usiamo il nome corretto del metodo del repository
        var storia = await _repository.GetStoriaFiltrataAsync(utenteId, search);
        return Ok(storia);
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
    public async Task<IActionResult> Update(int id, [FromBody] Transazione input)
    {
        // Assicurati che il metodo nel repository accetti (int, Transazione)
        var successo = await _repository.AggiornaTransazioneAsync(id, input);
        if (!successo) return NotFound();
        return Ok();
    }

    [HttpPost("registra")]
    public async Task<IActionResult> Registra([FromBody] RegistraTransazioneDto dto)
    {
        // 1. NON SERVE PIÙ il Deserialize! 
        // ASP.NET ha già trasformato il JSON in dto.TitoloLookup automaticamente.

        if (dto == null || dto.TitoloLookup == null)
            return BadRequest("Dati della transazione o del titolo mancanti.");

        try
        {
            // 2. Passa direttamente il DTO al repository
            var successo = await _repository.RegistraOperazioneCompletaAsync(dto);

            if (successo)
                return Ok(new { message = "Transazione registrata con successo" });

            return StatusCode(500, "Errore durante il salvataggio nel repository.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { errore = ex.Message });
        }
    }

}