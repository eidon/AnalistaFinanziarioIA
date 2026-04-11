using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

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
}