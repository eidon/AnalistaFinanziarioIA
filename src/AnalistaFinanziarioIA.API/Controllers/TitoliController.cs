using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitoliController : ControllerBase
{
    private readonly ITitoloRepository _titoloRepository;

    public TitoliController(ITitoloRepository titoloRepository)
    {
        _titoloRepository = titoloRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var titoli = await _titoloRepository.GetAllAsync();
        return Ok(titoli);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var titolo = await _titoloRepository.GetByIdAsync(id);
        return titolo is null ? NotFound() : Ok(titolo);
    }

    [HttpGet("simbolo/{simbolo}")]
    public async Task<IActionResult> GetBySimbolo(string simbolo)
    {
        var titolo = await _titoloRepository.GetBySimboloAsync(simbolo.ToUpperInvariant());
        return titolo is null ? NotFound() : Ok(titolo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Titolo titolo)
    {
        titolo.Simbolo = titolo.Simbolo.ToUpperInvariant();
        var created = await _titoloRepository.AddAsync(titolo);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Titolo titolo)
    {
        if (id != titolo.Id) return BadRequest();
        var existing = await _titoloRepository.GetByIdAsync(id);
        if (existing is null) return NotFound();
        var updated = await _titoloRepository.UpdateAsync(titolo);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _titoloRepository.DeleteAsync(id);
        return NoContent();
    }
}
