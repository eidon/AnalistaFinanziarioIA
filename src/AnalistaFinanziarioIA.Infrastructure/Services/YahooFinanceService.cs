using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;


namespace AnalistaFinanziarioIA.Infrastructure.Services;

public class YahooFinanceService : IYahooFinanceService
{
    private readonly ILogger<YahooFinanceService> _logger;
    private readonly HttpClient _httpClient;

    public YahooFinanceService(ILogger<YahooFinanceService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<decimal> GetQuoteAsync(string ticker)
    {
        string yahooTicker = string.Empty;
        try
        {

            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "VWCE",    "VWCE.DE"   },
                // Aggiungi qui altri se necessario
            };

            yahooTicker = mapping.TryGetValue(ticker, out var translated) ? translated : ticker;

            // Pulizia Ticker: rimuovi eventuali spazi bianchi
            yahooTicker = yahooTicker.Trim().ToUpper();

            // URL alternativo più robusto (query2 invece di query1)
            string url = $"https://query2.finance.yahoo.com/v8/finance/chart/{yahooTicker}?interval=1m&range=1d";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Header minimi per sembrare un browser reale ed evitare i 404/IOException
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Connection", "keep-alive");

            // Forza la chiusura della connessione dopo la richiesta per evitare IOException al giro dopo
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Ticker {yahooTicker} non trovato su Yahoo. Prova a cambiare suffisso nel DB.");
                return 0;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var price = result.GetProperty("meta").GetProperty("regularMarketPrice").GetDecimal();

            _logger.LogInformation($"[SUCCESS] {yahooTicker}: {price}");
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ERROR] {yahooTicker}: {ex.Message}");
            return 0;
        }
    }
}