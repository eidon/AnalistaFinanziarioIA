using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitoliController : ControllerBase
{
    private readonly ITitoloRepository _repository;
    private readonly IValidator<TitoloCreateDto> _validator; // Iniettiamo il validatore

    public TitoliController(ITitoloRepository repository, IValidator<TitoloCreateDto> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var titoli = await _repository.GetAllAsync();
        return Ok(titoli);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var titolo = await _repository.GetByIdAsync(id);
        return titolo is null ? NotFound() : Ok(titolo);
    }

    [HttpGet("simbolo/{simbolo}")]
    public async Task<IActionResult> GetBySimbolo(string simbolo)
    {
        var titolo = await _repository.GetBySimboloAsync(simbolo.ToUpperInvariant());
        return titolo is null ? NotFound() : Ok(titolo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TitoloCreateDto dto)
    {
        // Validazione manuale ma esplicita
        var validationResult = await _validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            // Restituisce un 400 Bad Request con la lista degli errori
            return BadRequest(validationResult.Errors);
        }

        // 1. Il server ha il controllo: creiamo l'oggetto Titolo partendo dai dati validati del DTO
        var nuovoTitolo = new Titolo
        {
            // Normalizziamo i dati critici
            Simbolo = dto.Simbolo.ToUpperInvariant().Trim(),
            Nome = dto.Nome.Trim(),
            Isin = dto.Isin?.ToUpperInvariant().Trim() ?? string.Empty,
            Valuta = dto.Valuta?.ToUpperInvariant() ?? "EUR",
            Mercato = dto.Mercato,
            Settore = dto.Settore
        };

        // 2. Passiamo al repository l'entità pulita
        var created = await _repository.AddAsync(nuovoTitolo);

        // 3. Restituiamo l'oggetto creato (o un ReadDto se vuoi essere super rigoroso)
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Titolo titolo)
    {
        if (id != titolo.Id) return BadRequest();
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return NotFound();
        var updated = await _repository.UpdateAsync(titolo);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("cerca")]
    public async Task<IActionResult> Cerca([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<Titolo>());

        // Chiamata al repository che esegue una query LIKE %query%
        var risultati = await _repository.CercaAsync(query);

        return Ok(risultati.Select(t => new {
            t.Id,
            t.Simbolo,
            t.Nome,
            t.Categoria, // es. "Azione", "ETF"
            t.Valuta
        }));
    }
}
