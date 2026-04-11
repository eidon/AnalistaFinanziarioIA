using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using System.Net.Http.Json;

namespace AnalistaFinanziarioIA.Core.Services
{
    public class QuotazioneService(HttpClient _httpClient, IQuotazioneRepository _repository, ITitoloRepository _titoloRepository, string apiKey) : IQuotazioneService
    {

        public async Task<decimal> AggiornaPrezzoTitoloAsync(int titoloId)
        {
            // 1. Cerchiamo il titolo nel database tramite il repository dei titoli
            var titolo = await _titoloRepository.GetByIdAsync(titoloId);

            if (titolo == null || string.IsNullOrEmpty(titolo.Simbolo))
            {
                return 0; // Titolo non trovato o senza ticker
            }

            string ticker = titolo.Simbolo;

            var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageResponse>(url);

            if (response?.GlobalQuote != null)
            {
                var prezzo = response.GlobalQuote.Price;

                // Salviamo la nuova quotazione nel database tramite il repository
                // Nota: Assicurati di avere questo metodo nel repository
                await _repository.SalvaQuotazioneAsync(titoloId, prezzo);

                return prezzo;
            }

            return 0;
        }

        //public async Task AggiornaTuttoIlPortafoglioAsync(Guid utenteId)
        //{
        //    var assets = await _repository.GetAssetsByUtenteIdAsync(utenteId);

        //    foreach (var asset in assets)
        //    {
        //        // 1. Chiamiamo l'aggiornamento per il singolo titolo
        //        await AggiornaPrezzoTitoloAsync(asset.TitoloId, asset.Titolo.Simbolo);

        //        // 2. Aspettiamo 12 secondi prima del prossimo titolo 
        //        // per non superare il limite di 5 chiamate al minuto di Alpha Vantage
        //        await Task.Delay(12000);
        //    }
        //}

    }
}