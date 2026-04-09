using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface ITitoloRepository
{
    Task<IEnumerable<Titolo>> GetAllAsync();
    Task<Titolo?> GetByIdAsync(int id);
    Task<Titolo?> GetBySimboloAsync(string simbolo);
    Task<Titolo> AddAsync(Titolo titolo);
    Task<Titolo> UpdateAsync(Titolo titolo);
    Task DeleteAsync(int id);

    Task<IEnumerable<Titolo>> CercaAsync(string query, int limit = 5);
}
