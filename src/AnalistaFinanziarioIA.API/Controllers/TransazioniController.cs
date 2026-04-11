using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TransazioniController(IPortafoglioRepository _repository) : ControllerBase
{
    // Recupera la storia per la sezione inferiore
    [HttpGet("utente/{utenteId}")]
    public async Task<IActionResult> GetStoria(Guid utenteId)
    {
        var storia = await _repository.GetTutteTransazioniUtenteAsync(utenteId);
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
    public async Task<IActionResult> Update(Guid id, [FromBody] TransazioneInputDto input)
    {
        var successo = await _repository.AggiornaTransazioneAsync(id, input);
        if (!successo) return NotFound();
        return Ok();
    }
}