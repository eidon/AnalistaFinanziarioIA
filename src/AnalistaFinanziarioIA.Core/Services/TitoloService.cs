using AnalistaFinanziarioIA.Core.DTOs; // Namespace aggiornato
using AnalistaFinanziarioIA.Core.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace AnalistaFinanziarioIA.Core.Services
{
    public class TitoloService(HttpClient _httpClient, string apiKey) : ITitoloService
    {
        public async Task<List<TitoloLookupDto>> CercaTitoliAsync(string query)
        {
            // URL corretto per la funzione SYMBOL_SEARCH
            var url = $"https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords={query}&apikey={apiKey}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<AlphaVantageSearchResponse>(url);

                if (response?.BestMatches == null) return new List<TitoloLookupDto>();

                return response.BestMatches.Select(m => new TitoloLookupDto
                {
                    Simbolo = m.Symbol,
                    Nome = m.Name,
                    Tipo = m.Type,
                    Valuta = m.Currency,
                    Regione = m.Region
                }).ToList();
            }
            catch (Exception)
            {
                return new List<TitoloLookupDto>();
            }
        }
    }
}
