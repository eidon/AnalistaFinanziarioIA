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

    public async Task SalvaQuotazioneAsync(int titoloId, decimal prezzo)
    {
        var adesso = DateTime.UtcNow;
        var oggi = adesso.Date;

        // --- PARTE 1: Gestione Storico (Una riga al giorno) ---
        var quotazioneGiorno = await _context.QuotazioniStoriche
            .FirstOrDefaultAsync(q => q.TitoloId == titoloId && q.Data.Date == oggi);

        if (quotazioneGiorno != null)
        {
            // Se il prezzo è diverso, aggiorniamo la riga di oggi
            quotazioneGiorno.PrezzoChiusura = prezzo;
            quotazioneGiorno.Data = adesso;
            _context.QuotazioniStoriche.Update(quotazioneGiorno);
        }
        else
        {
            // Nuovo giorno, nuova riga nello storico
            _context.QuotazioniStoriche.Add(new QuotazioneStorica
            {
                TitoloId = titoloId,
                PrezzoChiusura = prezzo,
                Data = adesso
            });
        }

        // --- PARTE 2: Aggiornamento Stato Corrente (Tabella Titoli) ---
        var titolo = await _context.Titoli.FindAsync(titoloId);
        if (titolo != null)
        {
            // Assicurati che il tuo modello 'Titolo' abbia una proprietà chiamata PrezzoAttuale o UltimoPrezzo
            // Se non l'hai ancora aggiunta, dovrai fare una migrazione o chiamarla come quella esistente
            titolo.UltimoPrezzo = prezzo;
            titolo.DataUltimoPrezzo = adesso;
            _context.Titoli.Update(titolo);
        }

        await _context.SaveChangesAsync();
    }
}
