using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace AnalistaFinanziarioIA.API.Plugins;

public class MercatoPlugin(IConfiguration configuration, IPortafoglioRepository repository)
{
    private readonly string _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new Exception("ApiKey AlphaVantage mancante!");
    private readonly HttpClient _httpClient = new();

    [KernelFunction]
    [Description("Recupera il prezzo attuale di mercato di un titolo (es. AAPL o VWCE) tramite il suo ticker.")]
    public async Task<string> GetPrezzoAttuale(string ticker)
    {
        // Nota: Alpha Vantage usa i ticker americani. Per gli ETF europei come VWCE
        // a volte serve aggiungere il suffisso della borsa, es: VWCE.DEX
        string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={_apiKey}";

        var response = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);

        // Estraiamo il prezzo dal JSON di Alpha Vantage
        if (doc.RootElement.TryGetProperty("Global Quote", out var quote) &&
            quote.TryGetProperty("05. price", out var priceElement))
        {
            return $"Il prezzo attuale di {ticker} è {priceElement.GetString()} USD/EUR.";
        }

        return $"Non sono riuscito a trovare il prezzo in tempo reale per {ticker}.";
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


}