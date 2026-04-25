using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface ITitoloService
{
    Task<List<TitoloLookupDto>> CercaTitoliAsync(string query);
    Task<List<TitoloLookupDto>> CercaPerIsinAsync(string isin);

    Task<TitoloLookupDto?> RecuperaDettagliCompletiAsync(string simbolo);

    TipoTitolo MappaTipoTitolo(string? fmpType, string? simbolo);
}