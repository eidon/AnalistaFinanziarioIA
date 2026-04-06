using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface IQuotazioneRepository
{
    Task<IEnumerable<QuotazioneStorica>> GetByTitoloIdAsync(int titoloId);
    Task<IEnumerable<QuotazioneStorica>> GetByTitoloIdAndPeriodoAsync(int titoloId, DateTime da, DateTime a);
    Task<QuotazioneStorica?> GetUltimaQuotazioneAsync(int titoloId);
    Task<QuotazioneStorica> AddAsync(QuotazioneStorica quotazione);
    Task AddRangeAsync(IEnumerable<QuotazioneStorica> quotazioni);
}
