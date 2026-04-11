using AnalistaFinanziarioIA.Core.DTOs;

namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface ITitoloService
{
    Task<List<TitoloLookupDto>> CercaTitoliAsync(string query);
}