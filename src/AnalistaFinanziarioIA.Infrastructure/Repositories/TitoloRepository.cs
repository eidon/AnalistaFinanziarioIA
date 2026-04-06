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
        _context.Titoli.Add(titolo);
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
}
