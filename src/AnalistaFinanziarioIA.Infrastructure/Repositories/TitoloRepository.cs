using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class TitoloRepository : ITitoloRepository
{
    private readonly AnalistaFinanziarioDbContext _context;

    public TitoloRepository(AnalistaFinanziarioDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Titolo>> GetAllAsync()
        => await _context.Titoli.ToListAsync();

    public async Task<Titolo?> GetByIdAsync(int id)
        => await _context.Titoli.FindAsync(id);

    public async Task<Titolo?> GetBySimboloAsync(string simbolo)
        => await _context.Titoli.FirstOrDefaultAsync(t => t.Simbolo == simbolo);

    public async Task<Titolo> AddAsync(Titolo titolo)
    {
        await _context.Titoli.AddAsync(titolo);
        await _context.SaveChangesAsync();
        return titolo;
    }

    public async Task<Titolo> UpdateAsync(Titolo titolo)
    {
        _context.Titoli.Update(titolo);
        await _context.SaveChangesAsync();
        return titolo;
    }

    public async Task DeleteAsync(int id)
    {
        var titolo = await _context.Titoli.FindAsync(id);
        if (titolo is not null)
        {
            _context.Titoli.Remove(titolo);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Titolo>> CercaAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Titolo>();

        var q = query.ToLower();
        return await _context.Titoli
            .Where(t => t.Simbolo.ToLower().Contains(q) || t.Nome.ToLower().Contains(q))
            .OrderBy(t => t.Nome) // Ordina alfabeticamente
            .Take(limit) // Non sovraccaricare la UI, i primi 5-10 bastano
            .ToListAsync();
    }

    public async Task<Titolo?> GetByIsinAsync(string isin)
    {
        if (string.IsNullOrWhiteSpace(isin)) return null;

        return await _context.Titoli
            .FirstOrDefaultAsync(t => t.Isin == isin);
    }

}
