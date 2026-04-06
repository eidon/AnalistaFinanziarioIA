using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/titoli/{titoloId:int}/[controller]")]
public class QuotazioniController : ControllerBase
{
    private readonly IQuotazioneRepository _quotazioneRepository;

    public QuotazioniController(IQuotazioneRepository quotazioneRepository)
    {
        _quotazioneRepository = quotazioneRepository;
    }

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
}
