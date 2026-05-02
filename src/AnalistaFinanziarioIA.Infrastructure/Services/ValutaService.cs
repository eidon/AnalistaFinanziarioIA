using System.Net.Http.Json;
using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.Infrastructure.Services
{
    public class ValutaService(HttpClient _httpClient, IYahooFinanceService _yahooService) : IValutaService
    {
        // Cache locale per non ripetere la stessa chiamata nella stessa richiesta
        private readonly Dictionary<string, decimal> _cacheTassi = new();

        public async Task<decimal> GetTassoCambioAsync(string da, string a = "EUR")
        {
            if (da == a) return 1.0m;

            // Se abbiamo già scaricato questo cambio in questa sessione, usalo!
            if (_cacheTassi.TryGetValue(da, out decimal value)) return value;

            try
            {
                // Frankfurter a volte richiede uno User-Agent per non chiudere la connessione
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AnalistaFinanziario/1.0)");

                var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(
                    $"https://api.frankfurter.app/latest?from={da}&to={a}");

                if (response?.Rates != null && response.Rates.TryGetValue(a, out decimal tasso))
                {
                    _cacheTassi[da] = tasso;
                    return tasso;
                }
            }
            catch
            { }
            // 2. FALLBACK SU YAHOO FINANCE (molto più robusto)
            try
            {
                // Costruiamo il simbolo per la valuta (es. USDEUR=X)
                string simboloValuta = $"{da}{a}=X".ToUpper();

                // Usiamo il metodo che hai già scritto in YahooFinanceService!
                decimal tassoYahoo = await _yahooService.GetQuoteAsync(simboloValuta);

                if (tassoYahoo > 0)
                {
                    _cacheTassi[da] = tassoYahoo;
                    return tassoYahoo;
                }
            }
            catch 
            { 
                /* Se fallisce anche Yahoo, andiamo al fallback finale */ 
            }
            
            return 1.0m;
        }

        public async Task<decimal> ConvertiInEurAsync(decimal importo, string valutaOriginale)
        {
            if (importo == 0) return 0;
            var tasso = await GetTassoCambioAsync(valutaOriginale, "EUR");
            return importo * tasso;
        }
    
    }
    internal class FrankfurterResponse
    {
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}