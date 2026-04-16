using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/titoli/{titoloId:int}/[controller]")]
public class QuotazioniController(IQuotazioneRepository _quotazioneRepository, IQuotazioneService _quotazioneService) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetByTitolo(int titoloId)
    {
        var quotazioni = await _quotazioneRepository.GetByTitoloIdAsync(titoloId);
        return Ok(quotazioni);
    }

    [HttpGet("periodo")]
    public async Task<IActionResult> GetByPeriodo(int titoloId, [FromQuery] DateTime da, [FromQuery] DateTime a)
    {
        var quotazioni = await _quotazioneRepository.GetByTitoloIdAndPeriodoAsync(titoloId, da, a);
        return Ok(quotazioni);
    }

    [HttpGet("ultima")]
    public async Task<IActionResult> GetUltima(int titoloId)
    {
        var quotazione = await _quotazioneRepository.GetUltimaQuotazioneAsync(titoloId);
        return quotazione is null ? NotFound() : Ok(quotazione);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int titoloId, [FromBody] QuotazioneStorica quotazione)
    {
        quotazione.TitoloId = titoloId;
        var created = await _quotazioneRepository.AddAsync(quotazione);
        return CreatedAtAction(nameof(GetUltima), new { titoloId }, created);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(int titoloId, [FromBody] IEnumerable<QuotazioneStorica> quotazioni)
    {
        foreach (var q in quotazioni)
        {
            q.TitoloId = titoloId;
        }
        await _quotazioneRepository.AddRangeAsync(quotazioni);
        return NoContent();
    }

    [HttpPost("aggiorna")]
    public async Task<IActionResult> AggiornaPrezzo(int titoloId)
    {
        var prezzo = await _quotazioneService.AggiornaPrezzoTitoloAsync(titoloId);

        if (prezzo > 0)
            return Ok(new { Messaggio = "Prezzo aggiornato con successo", Prezzo = prezzo });

        return BadRequest("Impossibile recuperare il prezzo. Controlla il ticker o il limite API.");
    }
}
