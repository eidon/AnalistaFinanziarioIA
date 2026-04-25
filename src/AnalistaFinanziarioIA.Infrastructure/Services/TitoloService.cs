using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnalistaFinanziarioIA.Infrastructure.Services
{
    public class TitoloService(HttpClient _httpClient, string apiKey) : ITitoloService
    {
        public async Task<List<TitoloLookupDto>> CercaTitoliAsync(string query)
        {
            var risultatiFinali = new List<TitoloLookupDto>();

            // 1. MOTORE FMP
            try
            {
                var urlFmp = $"https://financialmodelingprep.com/stable/search-name?query={query}&limit=10&apikey={apiKey}";
                ConfiguraUserAgent();
                var respFmp = await _httpClient.GetAsync(urlFmp);

                if (respFmp.IsSuccessStatusCode)
                {
                    var fmpData = await respFmp.Content.ReadFromJsonAsync<List<FmpSearchResult>>();
                    if (fmpData != null)
                    {
                        risultatiFinali.AddRange(fmpData.Select(t => new TitoloLookupDto
                        {
                            Simbolo = t.Symbol,
                            Nome = t.Name,
                            Valuta = t.Currency ?? "EUR",
                            Mercato = t.ExchangeShortName ?? t.StockExchange,
                            Tipo = MappaTipoTitolo(t.Type, t.Symbol).ToString()
                        }));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"FMP Error: {ex.Message}"); }

            // 2. MOTORE YAHOO (Integrazione per titoli mancanti come VWCE)
            try
            {
                var urlYahoo = $"https://query1.finance.yahoo.com/v1/finance/search?q={query}&quotesCount=10";
                var respYahoo = await _httpClient.GetAsync(urlYahoo);

                if (respYahoo.IsSuccessStatusCode)
                {
                    var jsonString = await respYahoo.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);
                    var quotes = doc.RootElement.GetProperty("quotes").EnumerateArray();

                    foreach (var q in quotes)
                    {
                        var simbolo = q.GetProperty("symbol").GetString() ?? string.Empty;

                        if (!risultatiFinali.Any(r => r.Simbolo.Equals(simbolo, StringComparison.OrdinalIgnoreCase)))
                        {
                            risultatiFinali.Add(new TitoloLookupDto
                            {
                                Simbolo = simbolo,
                                Nome = q.TryGetProperty("longname", out var ln) ? (ln.GetString() ?? string.Empty) : (q.GetProperty("shortname").GetString() ?? string.Empty),
                                Valuta = "EUR",
                                Mercato = q.TryGetProperty("exchDisp", out var exch) ? (exch.GetString() ?? "Yahoo") : "Yahoo",
                                Tipo = q.GetProperty("quoteType").GetString() == "ETF" ? "ETF" : "Azione"
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Yahoo Search Error: {ex.Message}"); }

            return risultatiFinali;
        }

        public async Task<TitoloLookupDto?> RecuperaDettagliCompletiAsync(string simbolo)
        {
            var urlProfile = $"https://financialmodelingprep.com/stable/profile?symbol={simbolo}&apikey={apiKey}";

            try
            {
                ConfiguraUserAgent();
                var response = await _httpClient.GetAsync(urlProfile);

                if (response.IsSuccessStatusCode)
                {
                    var profili = await response.Content.ReadFromJsonAsync<List<FmpFullProfile>>();
                    var p = profili?.FirstOrDefault();

                    if (p == null) return null;

                    return new TitoloLookupDto
                    {
                        Simbolo = p.Symbol ?? simbolo,
                        Nome = p.CompanyName ?? "N/A",
                        Isin = p.Isin ?? string.Empty,
                        Valuta = p.Currency ?? "EUR",
                        Mercato = p.ExchangeShortName ?? p.Exchange ?? "N/A",
                        Settore = string.IsNullOrEmpty(p.Industry) ? p.Sector ?? string.Empty : $"{p.Sector} - {p.Industry}",
                        Tipo = MappaTipoTitolo(p.Type, p.Symbol).ToString(),
                        PrezzoAttuale = p.Price
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FMP] Errore recupero profilo completo: {ex.Message}");
            }

            return null;
        }

        public async Task<List<TitoloLookupDto>> CercaPerIsinAsync(string isin)
        {
            var urlIsin = $"https://financialmodelingprep.com/stable/search-isin?isin={isin}&apikey={apiKey}";

            try
            {
                ConfiguraUserAgent();
                var risultatiIsin = await _httpClient.GetFromJsonAsync<List<FmpIsinResult>>(urlIsin);

                if (risultatiIsin == null || !risultatiIsin.Any()) return new List<TitoloLookupDto>();

                var listaDinamica = new List<TitoloLookupDto>();

                foreach (var res in risultatiIsin)
                {
                    var urlProfile = $"https://financialmodelingprep.com/stable/profile?symbol={res.Symbol}&apikey={apiKey}";
                    var responseProfile = await _httpClient.GetAsync(urlProfile);

                    if (!responseProfile.IsSuccessStatusCode)
                    {
                        listaDinamica.Add(new TitoloLookupDto
                        {
                            Simbolo = res.Symbol,
                            Nome = res.Name,
                            Isin = res.Isin,
                            Valuta = "EUR",
                            Mercato = "N/A",
                            Tipo = "Azione"
                        });
                        continue;
                    }

                    var profili = await responseProfile.Content.ReadFromJsonAsync<List<FmpFullProfile>>();
                    var p = profili?.FirstOrDefault();

                    listaDinamica.Add(new TitoloLookupDto
                    {
                        Simbolo = res.Symbol,
                        Nome = res.Name,
                        Isin = res.Isin,
                        Valuta = p?.Currency ?? "EUR",
                        Mercato = p?.ExchangeShortName ?? p?.Exchange ?? "N/A",
                        Tipo = MappaTipoTitolo(p?.Type, res.Symbol).ToString()
                    });
                }

                return listaDinamica;
            }
            catch { return new List<TitoloLookupDto>(); }
        }

        public TipoTitolo MappaTipoTitolo(string? fmpType, string? simbolo)
        {
            if (string.IsNullOrEmpty(fmpType))
            {
                if (simbolo != null && simbolo.StartsWith("VWCE", StringComparison.OrdinalIgnoreCase))
                    return TipoTitolo.ETF;
                return TipoTitolo.Azione;
            }

            return fmpType.ToLower().Trim() switch
            {
                "stock" or "equity" or "common stock" => TipoTitolo.Azione,
                "etf" or "trust" or "fund" or "mutual_fund" or "mutualfund" => TipoTitolo.ETF,
                "cryptocurrency" or "crypto" => TipoTitolo.Crypto,
                _ => TipoTitolo.Azione
            };
        }

        private void ConfiguraUserAgent()
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.34.0");
            }
        }

        // ── Classi adattatore per le API esterne FMP ────────────────────────

        public class FmpFullProfile
        {
            [JsonPropertyName("symbol")] public string? Symbol { get; set; }
            [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
            [JsonPropertyName("isin")] public string? Isin { get; set; }
            [JsonPropertyName("currency")] public string? Currency { get; set; }
            [JsonPropertyName("exchangeShortName")] public string? ExchangeShortName { get; set; }
            [JsonPropertyName("exchange")] public string? Exchange { get; set; }
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("sector")] public string? Sector { get; set; }
            [JsonPropertyName("industry")] public string? Industry { get; set; }
            [JsonPropertyName("price")] public decimal Price { get; set; }
        }

        public class FmpSearchResult
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("currency")] public string? Currency { get; set; }
            [JsonPropertyName("stockExchange")] public string StockExchange { get; set; } = string.Empty;
            [JsonPropertyName("exchangeShortName")] public string? ExchangeShortName { get; set; }
            [JsonPropertyName("type")] public string? Type { get; set; }
        }

        public class FmpIsinResult
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("isin")] public string Isin { get; set; } = string.Empty;
        }
    }
}
