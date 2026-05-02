using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using System.Net.Http.Json;

namespace AnalistaFinanziarioIA.API.Plugins;

public class MercatoPlugin(
    IConfiguration _configuration,
    IYahooFinanceService _yahooService,
    ITitoloRepository _titoloRepo,
    IHttpClientFactory _httpClientFactory)
{
    private readonly HttpClient _httpClient = _httpClientFactory.CreateClient();

    [KernelFunction("GetAndamentoStorico")]
    [Description("Ottiene i dati storici di chiusura di un titolo per un determinato numero di giorni. Utile per analizzare trend, volatilità e performance passate.")]
    public async Task<string> GetAndamentoStorico(
    [Description("Il ticker del titolo (es. LDO.MI, AAPL, VWCE.DE)")] string ticker,
    [Description("Numero di giorni passati da analizzare (default 30)")] int giorni = 30)
    {
        try
        {
            var storia = await _yahooService.GetHistoryAsync(ticker, giorni);

            if (storia == null || !storia.Any())
                return $"Non sono riuscito a recuperare i dati storici per {ticker}.";

            var listaDati = storia.ToList();
            var primo = listaDati.First();
            var ultimo = listaDati.Last();
            var variazioneAssoluta = ultimo.PrezzoChiusura - primo.PrezzoChiusura;
            var variazionePercentuale = (variazioneAssoluta / primo.PrezzoChiusura) * 100;

            // Prepariamo una sintesi per l'IA
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Analisi storica per {ticker} negli ultimi {giorni} giorni:");
            sb.AppendLine($"- Prezzo iniziale ({primo.Data:dd/MM/yyyy}): {primo.PrezzoChiusura:N2}");
            sb.AppendLine($"- Prezzo attuale ({ultimo.Data:dd/MM/yyyy}): {ultimo.PrezzoChiusura:N2}");
            sb.AppendLine($"- Performance nel periodo: {variazionePercentuale:+0.00;-0.00}%");

            // Passiamo anche i dati grezzi in formato compatto così l'IA può "vederli" se serve
            sb.AppendLine("\nDati giornalieri (Data: Chiusura):");
            foreach (var p in listaDati.TakeLast(10)) // Mostriamo solo gli ultimi 10 per non intasare il contesto
            {
                sb.AppendLine($"{p.Data:dd/MM}: {p.PrezzoChiusura:N2}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Errore durante l'analisi storica di {ticker}: {ex.Message}";
        }
    }

    [KernelFunction("GetPrezzoAttuale")]
    [Description("Recupera il prezzo attuale di mercato di un titolo (es. AAPL o VWCE.DE) tramite il suo ticker.")]
    public async Task<string> GetPrezzoAttuale(string ticker)
    {
        Console.WriteLine($"[IA DEBUG] Richiesta prezzo per: '{ticker}'");

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
            var titolo = await _titoloRepo.GetBySimboloAsync(simbolo);

            if (titolo == null)
                return $"Titolo {simbolo} non trovato nel database. Verificare il ticker.";

            titolo.UltimoPrezzo = nuovoPrezzo;
            titolo.DataUltimoPrezzo = DateTime.UtcNow;

            await _titoloRepo.UpdateAsync(titolo);

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
            // Recupero gerarchico
            string apiKey = _configuration["ExchangeRate:ApiKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("[ERRORE] ExchangeRate:ApiKey non trovata!");
                return 0.92m;
            }

            var response = await _httpClient.GetFromJsonAsync<JsonElement>(
                $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD");

            var rates = response.GetProperty("conversion_rates");
            return rates.GetProperty("EUR").GetDecimal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IA DEBUG] Errore cambio reale: {ex.Message}");
            return 0.92m; // Fallback
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
