using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class AnalisiRepository : IAnalisiRepository
{
    private readonly AnalistaFinanziarioDbContext _context;

    public AnalisiRepository(AnalistaFinanziarioDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AnalisiFinanziaria>> GetByTitoloIdAsync(int titoloId)
        => await _context.Analisi
            .Where(a => a.TitoloId == titoloId)
            .OrderByDescending(a => a.DataAnalisi)
            .ToListAsync();

    public async Task<AnalisiFinanziaria?> GetByIdAsync(int id)
        => await _context.Analisi.FindAsync(id);

    public async Task<AnalisiFinanziaria> AddAsync(AnalisiFinanziaria analisi)
    {
        _context.Analisi.Add(analisi);
        await _context.SaveChangesAsync();
        return analisi;
    }

    public async Task<AnalisiFinanziaria> UpdateAsync(AnalisiFinanziaria analisi)
    {
        _context.Analisi.Update(analisi);
        await _context.SaveChangesAsync();
        return analisi;
    }

    public async Task DeleteAsync(int id)
    {
        var analisi = await _context.Analisi.FindAsync(id);
        if (analisi is not null)
        {
            _context.Analisi.Remove(analisi);
            await _context.SaveChangesAsync();
        }
    }
}
