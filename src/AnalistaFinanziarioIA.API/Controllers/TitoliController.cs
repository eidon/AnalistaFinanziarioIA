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
    private readonly IValidator<TitoloCreateDto> _validator;
    private readonly ITitoloService _titoloService;

    public TitoliController(ITitoloRepository repository, IValidator<TitoloCreateDto> validator, ITitoloService titoloService)
    {
        _repository = repository;
        _validator = validator;
        _titoloService = titoloService;
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
        var validationResult = await _validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var nuovoTitolo = new Titolo
        {
            Simbolo = dto.Simbolo.ToUpperInvariant().Trim(),
            Nome = dto.Nome.Trim(),
            Isin = dto.Isin?.ToUpperInvariant().Trim() ?? string.Empty,
            Valuta = dto.Valuta?.ToUpperInvariant() ?? "EUR",
            Mercato = dto.Mercato,
            Settore = dto.Settore,
        };

        var created = await _repository.AddAsync(nuovoTitolo);

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

    [HttpGet("dettaglio-completo/{simbolo}")]
    public async Task<IActionResult> GetDettaglioCompleto(string simbolo)
    {
        var risultato = await _titoloService.RecuperaDettagliCompletiAsync(simbolo);
        if (risultato == null) return NotFound();
        return Ok(risultato);
    }

    // Ricerca nel database locale
    [HttpGet("cerca")]
    public async Task<IActionResult> Cerca([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<Titolo>());

        var risultati = await _repository.CercaAsync(query);

        return Ok(risultati.Select(t => new {
            t.Id,
            t.Simbolo,
            t.Nome,
            t.Tipo,
            t.Valuta
        }));
    }

    // Ricerca tramite API esterne (FMP + Yahoo Finance) — route preservata per compatibilità frontend
    [HttpGet]
    [Route("/api/titolo/cerca")]
    public async Task<IActionResult> CercaEsterno([FromQuery] string q, [FromQuery] string tipo = "nome")
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<TitoloLookupDto>());

        try
        {
            if (tipo == "isin")
            {
                var risultati = await _titoloService.CercaPerIsinAsync(q);
                return Ok(risultati);
            }

            var baseRisultati = await _titoloService.CercaTitoliAsync(q);
            return Ok(baseRisultati);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Si è verificato un errore interno durante la ricerca tramite API esterne (FMP + Yahoo Finance).");
        }
    }
}
