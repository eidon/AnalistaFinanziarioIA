using AnalistaFinanziarioIA.Core.DTOs; // Namespace aggiornato
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AnalistaFinanziarioIA.Core.Services
{
    public class TitoloService(HttpClient _httpClient, string apiKey) : ITitoloService
    {
        public async Task<List<TitoloLookupDto>> CercaTitoliAsync(string query)
        {

            // Endpoint di ricerca generale di FMP

            var url = $"https://financialmodelingprep.com/stable/search-name?query={query}&limit=10&apikey={apiKey}";

            try
            {
                ConfiguraUserAgent();
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Se ricevi 403, leggi il contenuto per capire perché
                    var errore = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Errore API: {response.StatusCode} - {errore}");
                    return new List<TitoloLookupDto>();
                }

                var risultati = await response.Content.ReadFromJsonAsync<List<FmpSearchResult>>();

                if (risultati == null) return new List<TitoloLookupDto>();

                var convert =  risultati.Select(t => new TitoloLookupDto
                {
                    Simbolo = t.Symbol,
                    Nome = t.Name,
                    Valuta = t.Currency ?? "EUR",
                    Mercato = t.ExchangeShortName ?? t.StockExchange,
                    // Convertiamo il tipo di FMP nel tuo Enum TipoTitolo (come stringa per il DTO)
                    Tipo = MappaTipoTitolo(t.Type, t.Symbol).ToString()
                }).ToList();


                return convert;
            }
            catch (Exception ex)
            {
                // Logga l'errore se necessario
                return new List<TitoloLookupDto>();
            }
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
                        Isin = p.Isin,
                        Valuta = p.Currency ?? "EUR",
                        Mercato = p.ExchangeShortName ?? p.Exchange ?? "N/A",
                        // Uniamo Settore e Industria per avere dati precisi come nel tuo DB (es. "Industrials - Aerospace & Defense")
                        Settore = string.IsNullOrEmpty(p.Industry) ? p.Sector : $"{p.Sector} - {p.Industry}",
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
                    // 2. Per ogni simbolo trovato, cerchiamo il profilo completo
                    // Usiamo 'stable' e la tua nuova classe FmpFullProfile
                    var urlProfile = $"https://financialmodelingprep.com/stable/profile?symbol={res.Symbol}&apikey={apiKey}";

                    var responseProfile = await _httpClient.GetAsync(urlProfile);

                    // Se il profilo dà 402, usiamo i dati base del risultato ISIN e valori di default
                    if (!responseProfile.IsSuccessStatusCode)
                    {
                        listaDinamica.Add(new TitoloLookupDto
                        {
                            Simbolo = res.Symbol,
                            Nome = res.Name,
                            Isin = res.Isin,
                            Valuta = "EUR", // Default prudenziale
                            Mercato = "N/A",
                            Tipo = "Azione"
                        });
                        continue;
                    }

                    var profili = await responseProfile.Content.ReadFromJsonAsync<List<FmpFullProfile>>();
                    var p = profili?.FirstOrDefault();

                    // 3. Mappiamo i dati reali dal profilo
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

        public TipoTitolo MappaTipoTitolo(string? fmpType, string simbolo)
        {
            if (string.IsNullOrEmpty(fmpType))
            {
                // Se FMP non ci dà il tipo, proviamo a indovinare dal simbolo
                if (simbolo.Contains(".MI") || simbolo.Length <= 5) return TipoTitolo.Azione;
                return TipoTitolo.Azione; // Default
            }

            return fmpType.ToLower() switch
            {
                "stock" => TipoTitolo.Azione,
                "trust" or "etf" or "fund" or "mutual_fund" => TipoTitolo.ETF,
                "cryptocurrency" => TipoTitolo.Crypto,
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


        // Classi di supporto per il Profile
        public class FmpFullProfile
        {
            [JsonPropertyName("symbol")] public string? Symbol { get; set; }
            [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
            [JsonPropertyName("isin")] public string? Isin { get; set; }
            [JsonPropertyName("currency")] public string? Currency { get; set; }
            [JsonPropertyName("exchangeShortName")] public string? ExchangeShortName { get; set; }
            [JsonPropertyName("exchange")] public string? Exchange { get; set; }
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("sector")] public string? Sector { get; set; } // <--- AGGIUNTO
            [JsonPropertyName("industry")] public string? Industry { get; set; } // <--- AGGIUNTO
            [JsonPropertyName("price")] public decimal Price { get; set; } // <--- AGGIUNTO
        }

        // Classe di supporto per il mapping della risposta FMP
        public class FmpSearchResult
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("currency")] public string? Currency { get; set; }
            [JsonPropertyName("stockExchange")] public string StockExchange { get; set; } = string.Empty;
            [JsonPropertyName("exchangeShortName")] public string? ExchangeShortName { get; set; }
            [JsonPropertyName("type")] public string? Type { get; set; } // "stock", "etf", "trust", ecc.
        }

        public class FmpIsinResult
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("isin")] public string Isin { get; set; } = string.Empty;
        }

    }

}
