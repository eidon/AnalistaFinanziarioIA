using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using System.Net.Http.Json;

namespace AnalistaFinanziarioIA.API.Plugins;

public class MercatoPlugin(
    IConfiguration configuration,
    IYahooFinanceService yahooService,
    ITitoloRepository titoloRepo,
    IHttpClientFactory httpClientFactory)
{
    private readonly string _apiKey = configuration["AlphaVantage:ApiKey"]
        ?? throw new InvalidOperationException("AlphaVantage:ApiKey non configurata.");
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [KernelFunction("GetPrezzoAttuale")]
    [Description("Recupera il prezzo attuale di mercato di un titolo (es. AAPL o VWCE.DE) tramite il suo ticker.")]
    public async Task<string> GetPrezzoAttuale(string ticker)
    {
        Console.WriteLine($"[IA DEBUG] Richiesta prezzo per: '{ticker}'");

        decimal prezzo = await yahooService.GetQuoteAsync(ticker);

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
            var titolo = await titoloRepo.GetBySimboloAsync(simbolo);

            if (titolo == null)
                return $"Titolo {simbolo} non trovato nel database. Verificare il ticker.";

            titolo.UltimoPrezzo = nuovoPrezzo;
            titolo.DataUltimoPrezzo = DateTime.UtcNow;

            await titoloRepo.UpdateAsync(titolo);

            return $"Database aggiornato: {simbolo} ora quota {nuovoPrezzo}.";
        }
        catch (Exception ex)
        {
            return $"Errore tecnico durante l'aggiornamento di {simbolo}: {ex.Message}";
        }
    }

    [KernelFunction("GetTassoCambio")]
    [Description("Recupera il tasso di cambio attuale tra due valute (es. USD to EUR).")]
    public Task<decimal> GetTassoCambio(string da, string a)
    {
        if (da == "USD" && a == "EUR") return Task.FromResult(0.92m);
        if (da == "EUR" && a == "USD") return Task.FromResult(1.08m);
        return Task.FromResult(1.0m);
    }

    [KernelFunction("GetTassoCambioReale")]
    [Description("Recupera il tasso di cambio attuale da USD a EUR tramite API.")]
    public async Task<decimal> GetTassoCambioReale()
    {
        try
        {
            string apiKey = configuration["ExchangeRateApiKey"] ?? string.Empty;

            var response = await _httpClient.GetFromJsonAsync<JsonElement>(
                $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD");

            var rates = response.GetProperty("conversion_rates");
            return rates.GetProperty("EUR").GetDecimal();
        }
        catch
        {
            return 0.92m;
        }
    }

    [KernelFunction("ConvertiValuta")]
    [Description("Converte un importo da una valuta all'altra.")]
    public decimal ConvertiValuta(decimal importo, string da, string a)
    {
        decimal eurUsd = 1.08m;

        if (da == "USD" && a == "EUR") return importo / eurUsd;
        if (da == "EUR" && a == "USD") return importo * eurUsd;

        return importo;
    }
}
