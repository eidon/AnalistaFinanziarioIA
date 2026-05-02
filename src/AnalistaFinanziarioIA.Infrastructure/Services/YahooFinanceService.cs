using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models; // Assicurati che QuotazioneStorica sia qui
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace AnalistaFinanziarioIA.Infrastructure.Services;

public class YahooFinanceService(ILogger<YahooFinanceService> _logger, HttpClient _httpClient) : IYahooFinanceService
{
    // Usiamo ConcurrentDictionary per evitare errori se due utenti chiedono dati contemporaneamente
    private static readonly ConcurrentDictionary<string, (DateTime Scadenza, List<QuotazioneStorica> Dati)> _cacheStorica = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

    private static string MappaSimbolo(string simbolo)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "VWCE", "VWCE.DE" },
            // Aggiungi qui altri se necessario
        };

        string mapped = mapping.TryGetValue(simbolo, out var translated) ? translated : simbolo;
        return mapped.Trim().ToUpper();
    }

    private static void ApplicaHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Headers.Add("Connection", "keep-alive");
    }

    public async Task<decimal> GetQuoteAsync(string simbolo)
    {
        string yahooSimbol = MappaSimbolo(simbolo);
        try
        {
            // Applichiamo la stessa logica di fallback anche qui se vuoi essere super sicuro, 
            // ma query2 per le singole quote solitamente è stabile.
            string url = $"https://query2.finance.yahoo.com/v8/finance/chart/{yahooSimbol}?interval=1m&range=1d";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplicaHeaders(request);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Simbolo {YahooTicker} non trovato su Yahoo.", yahooSimbol);
                return 0;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var price = result.GetProperty("meta").GetProperty("regularMarketPrice").GetDecimal();

            _logger.LogInformation("[SUCCESS] {YahooTicker}: {Price}", yahooSimbol, price);
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ERROR] {YahooTicker}: {ErrorMessage}", yahooSimbol, ex.Message);
            return 0;
        }
    }

    public async Task<IEnumerable<QuotazioneStorica>> GetHistoryAsync(string simbolo, int giorni = 30)
    {
        string yahooSimbol = MappaSimbolo(simbolo);

        // 1. TENTA IL RECUPERO DALLA CACHE
        if (_cacheStorica.TryGetValue(yahooSimbol, out var cacheHit) && DateTime.Now < cacheHit.Scadenza)
        {
            _logger.LogInformation("[CACHE HIT] {YahooTicker}: dati recuperati dalla memoria", yahooSimbol);
            return cacheHit.Dati;
        }

        string[] baseUrls = [
            "https://query1.finance.yahoo.com/v8/finance/chart/",
            "https://query2.finance.yahoo.com/v8/finance/chart/"
        ];

        long dataFine = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long dataInizio = DateTimeOffset.UtcNow.AddDays(-giorni).ToUnixTimeSeconds();

        foreach (var baseUrl in baseUrls)
        {
            try
            {
                string url = $"{baseUrl}{yahooSimbol}?period1={dataInizio}&period2={dataFine}&interval=1d";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Version = new Version(1, 1);
                ApplicaHeaders(request);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                if (!response.IsSuccessStatusCode) continue;

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                    !chart.TryGetProperty("result", out var resultList) ||
                    resultList.GetArrayLength() == 0) continue;

                var result = resultList[0];
                if (!result.TryGetProperty("timestamp", out var timestampProp)) continue;

                var timestamps = timestampProp.EnumerateArray().ToList();
                var prices = result.GetProperty("indicators").GetProperty("adjclose")[0].GetProperty("adjclose").EnumerateArray().ToList();

                var history = new List<QuotazioneStorica>();
                for (int i = 0; i < timestamps.Count; i++)
                {
                    if (i < prices.Count && prices[i].ValueKind == JsonValueKind.Number)
                    {
                        history.Add(new QuotazioneStorica
                        {
                            Data = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).DateTime,
                            PrezzoChiusura = prices[i].GetDecimal()
                        });
                    }
                }

                if (history.Count != 0)
                {
                    // 3. SALVA NELLA CACHE USANDO LA VARIABILE _cacheDuration
                    _cacheStorica[yahooSimbol] = (DateTime.Now.Add(_cacheDuration), history);

                    _logger.LogInformation("[HISTORY SUCCESS] {YahooTicker}: recuperati {Count} punti da {Host}",
                        yahooSimbol, history.Count, new Uri(baseUrl).Host);
                    return history;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[HISTORY RETRY] Fallito tentativo su {Server}: {Msg}", baseUrl, ex.Message);
            }
        }

        _logger.LogError("[HISTORY FATAL] {YahooTicker}: Impossibile recuperare dati da tutti i server.", yahooSimbol);
        return [];
    }


}