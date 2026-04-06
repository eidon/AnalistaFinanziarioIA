using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class QuotazioneRepository : IQuotazioneRepository
{
    private readonly AnalistaFinanziarioDbContext _context;

    public QuotazioneRepository(AnalistaFinanziarioDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<QuotazioneStorica>> GetByTitoloIdAsync(int titoloId)
        => await _context.QuotazioniStoriche
            .Where(q => q.TitoloId == titoloId)
            .OrderByDescending(q => q.Data)
            .ToListAsync();

    public async Task<IEnumerable<QuotazioneStorica>> GetByTitoloIdAndPeriodoAsync(
        int titoloId, DateTime da, DateTime a)
        => await _context.QuotazioniStoriche
            .Where(q => q.TitoloId == titoloId && q.Data >= da && q.Data <= a)
            .OrderBy(q => q.Data)
            .ToListAsync();

    public async Task<QuotazioneStorica?> GetUltimaQuotazioneAsync(int titoloId)
        => await _context.QuotazioniStoriche
            .Where(q => q.TitoloId == titoloId)
            .OrderByDescending(q => q.Data)
            .FirstOrDefaultAsync();

    public async Task<QuotazioneStorica> AddAsync(QuotazioneStorica quotazione)
    {
        _context.QuotazioniStoriche.Add(quotazione);
        await _context.SaveChangesAsync();
        return quotazione;
    }

    public async Task AddRangeAsync(IEnumerable<QuotazioneStorica> quotazioni)
    {
        _context.QuotazioniStoriche.AddRange(quotazioni);
        await _context.SaveChangesAsync();
    }
}
