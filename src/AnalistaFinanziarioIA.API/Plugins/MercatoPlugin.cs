using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace AnalistaFinanziarioIA.API.Plugins;

public class MercatoPlugin(IConfiguration configuration, IPortafoglioRepository repository, IYahooFinanceService _yahooService)
{
    private readonly string _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new Exception("ApiKey AlphaVantage mancante!");
    private readonly HttpClient _httpClient = new();

    //[KernelFunction("GetPrezzoAttuale")]
    //[Description("Recupera il prezzo attuale di mercato di un titolo (es. AAPL o VWCE) tramite il suo ticker.")]
    //public async Task<string> GetPrezzoAttuale(string ticker)
    //{
    //    // Nota: Alpha Vantage usa i ticker americani. Per gli ETF europei come VWCE
    //    // a volte serve aggiungere il suffisso della borsa, es: VWCE.DEX
    //    string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={_apiKey}";

    //    var response = await _httpClient.GetStringAsync(url);
    //    using var doc = JsonDocument.Parse(response);

    //    // Estraiamo il prezzo dal JSON di Alpha Vantage
    //    if (doc.RootElement.TryGetProperty("Global Quote", out var quote) &&
    //        quote.TryGetProperty("05. price", out var priceElement))
    //    {
    //        return $"Il prezzo attuale di {ticker} è {priceElement.GetString()} USD/EUR.";
    //    }

    //    return $"Non sono riuscito a trovare il prezzo in tempo reale per {ticker}.";
    //}

    [KernelFunction("GetPrezzoAttuale")]
    [Description("Recupera il prezzo attuale di mercato di un titolo (es. AAPL o VWCE.DE) tramite il suo ticker.")]
    public async Task<string> GetPrezzoAttuale(string ticker)
    {

        Console.WriteLine($"[IA DEBUG] Richiesta prezzo per: '{ticker}'");

        // Usiamo il servizio Yahoo che abbiamo appena "blindato"
        decimal prezzo = await _yahooService.GetQuoteAsync(ticker);

        if (prezzo > 0)
        {
            return $"Il prezzo attuale di {ticker} è {prezzo:N2} EUR/USD.";
        }

        return $"Non sono riuscito a recuperare il prezzo in tempo reale per {ticker}. Verifica se il ticker è corretto (es. aggiungere .MI o .DE).";
    }

    [KernelFunction("AggiornaPrezzoTitolo")]
    [Description("Aggiorna il prezzo di mercato (UltimoPrezzo) di un titolo nel database.")]
    public async Task<string> AggiornaPrezzoTitolo(string simbolo, decimal nuovoPrezzo)
    {
        try
        {
            // 1. Cerchiamo il titolo nel DB tramite il simbolo (es. NVDA)
            var titolo = await repository.GetTitoloBySimboloAsync(simbolo);

            if (titolo == null)
                return $"Titolo {simbolo} non trovato nel database. Verificare il ticker.";

            // 2. Aggiorniamo il campo che abbiamo creato con la migrazione
            titolo.UltimoPrezzo = nuovoPrezzo;

            // 3. Salviamo le modifiche
            await repository.UpdateTitoloAsync(titolo);

            return $"Database aggiornato: {simbolo} ora quota {nuovoPrezzo}.";
        }
        catch (Exception ex)
        {
            return $"Errore tecnico durante l'aggiornamento di {simbolo}: {ex.Message}";
        }
    }

    [KernelFunction("GetTassoCambio")]
    [Description("Recupera il tasso di cambio attuale tra due valute (es. USD to EUR).")]
    public async Task<decimal> GetTassoCambio(string da, string a)
    {
        // Per ora usiamo un valore fisso, ma pronto per l'API
        if (da == "USD" && a == "EUR") return 0.92m;
        if (da == "EUR" && a == "USD") return 1.08m;
        return 1.0m;
    }

    [KernelFunction("GetTassoCambioReale")]
    [Description("Recupera il tasso di cambio attuale da USD a EUR tramite API.")]
    public async Task<decimal> GetTassoCambioReale()
    {
        try
        {
            // Recupera la chiave che abbiamo appena messo in appsettings
            string apiKey = configuration["ExchangeRateApiKey"];
            using HttpClient client = new HttpClient();

            // Usiamo il tuo percorso: /latest/USD
            var response = await client.GetFromJsonAsync<JsonElement>(
                $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD");

            // Navighiamo nel JSON: conversion_rates -> EUR
            var rates = response.GetProperty("conversion_rates");
            decimal tassoEur = rates.GetProperty("EUR").GetDecimal();

            return tassoEur;
        }
        catch (Exception)
        {
            // Se l'API non risponde, restituiamo il valore di fallback 
            // per non bloccare l'intera Dashboard
            return 0.92m;
        }
    }

    [KernelFunction("GetPrezzoRealeMercato")]
    [Description("Cerca il prezzo attuale di un titolo sul mercato finanziario globale.")]
    public async Task<decimal> GetPrezzoRealeMercato(string simbolo)
    {
        // Qui in futuro metteremo la chiamata a Alpha Vantage o Yahoo
        // Per ora, facciamo un piccolo switch per testare la coerenza
        return simbolo switch
        {
            "NVDA" => 915.00m,
            "VWCE" => 112.40m,
            _ => 0m
        };
    }

    [KernelFunction("ConvertiValuta")]
    [Description("Converte un importo da una valuta all'altra.")]
    public decimal ConvertiValuta(decimal importo, string da, string a)
    {
        // Tasso di cambio ipotetico (1 EUR = 1.08 USD)
        decimal eurUsd = 1.08m;

        if (da == "USD" && a == "EUR") return importo / eurUsd;
        if (da == "EUR" && a == "USD") return importo * eurUsd;

        return importo; // Se le valute sono uguali
    }

}