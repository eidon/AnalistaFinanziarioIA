using AnalistaFinanziarioIA.Core.DTOs;
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
    public async Task<IActionResult> Create(int titoloId, [FromBody] AnalisiCreateDto dto)
    {
        // Creiamo l'entità internamente: il server ha il controllo totale
        var analisi = new AnalisiFinanziaria
        {
            TitoloId = titoloId,           // Preso dall'URL (sicuro)
            DataAnalisi = DateTime.UtcNow, // Decisa dal server (sicura)
            Risultato = dto.Risultato,
            PrezzoTarget = dto.PrezzoTarget,
            Raccomandazione = dto.Raccomandazione,
            Note = dto.Note
        };

        var created = await _analisiRepository.AddAsync(analisi);

        // Tipicamente qui restituiresti un AnalisiReadDto per non esporre l'intera entità
        return CreatedAtAction(nameof(GetById), new { titoloId, id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int titoloId, int id, [FromBody] AnalisiUpdateDto dto)
    {
        // 1. Recupera l'analisi esistente
        var esistente = await _analisiRepository.GetByIdAsync(id);

        // 2. Controllo coerenza: esiste? appartiene a questo titolo?
        if (esistente is null || esistente.TitoloId != titoloId)
            return NotFound("Analisi non trovata per questo titolo");

        // 3. Aggiorna solo i campi che il DTO permette di modificare
        esistente.Risultato = dto.Risultato;
        esistente.PrezzoTarget = dto.PrezzoTarget;
        esistente.Raccomandazione = dto.Raccomandazione;
        esistente.Note = dto.Note;
        // NON aggiorniamo TitoloId, Id o DataAnalisi: restano quelli originali.

        var updated = await _analisiRepository.UpdateAsync(esistente);
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
