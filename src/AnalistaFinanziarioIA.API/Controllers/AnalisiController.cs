using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/titoli/{titoloId:int}/[controller]")]
public class AnalisiController : ControllerBase
{
    private readonly IAnalisiRepository _analisiRepository;

    public AnalisiController(IAnalisiRepository analisiRepository)
    {
        _analisiRepository = analisiRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetByTitolo(int titoloId)
    {
        var analisi = await _analisiRepository.GetByTitoloIdAsync(titoloId);
        return Ok(analisi);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int titoloId, int id)
    {
        var analisi = await _analisiRepository.GetByIdAsync(id);
        if (analisi is null || analisi.TitoloId != titoloId) return NotFound();
        return Ok(analisi);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int titoloId, [FromBody] AnalisiFinanziaria analisi)
    {
        analisi.TitoloId = titoloId;
        var created = await _analisiRepository.AddAsync(analisi);
        return CreatedAtAction(nameof(GetById), new { titoloId, id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int titoloId, int id, [FromBody] AnalisiFinanziaria analisi)
    {
        if (id != analisi.Id || titoloId != analisi.TitoloId) return BadRequest();
        var updated = await _analisiRepository.UpdateAsync(analisi);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int titoloId, int id)
    {
        var analisi = await _analisiRepository.GetByIdAsync(id);
        if (analisi is null || analisi.TitoloId != titoloId) return NotFound();
        await _analisiRepository.DeleteAsync(id);
        return NoContent();
    }
}
