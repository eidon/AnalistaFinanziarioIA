using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface IAnalisiRepository
{
    Task<IEnumerable<AnalisiFinanziaria>> GetByTitoloIdAsync(int titoloId);
    Task<AnalisiFinanziaria?> GetByIdAsync(int id);
    Task<AnalisiFinanziaria> AddAsync(AnalisiFinanziaria analisi);
    Task<AnalisiFinanziaria> UpdateAsync(AnalisiFinanziaria analisi);
    Task DeleteAsync(int id);
}
