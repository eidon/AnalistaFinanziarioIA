using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IYahooFinanceService
    {
        /// <summary>
        /// Recupera il prezzo attuale per un determinato ticker.
        /// </summary>
        Task<decimal> GetQuoteAsync(string simbolo);

        // Aggiungiamo questo per recuperare i dati storici da Yahoo
        Task<IEnumerable<QuotazioneStorica>> GetHistoryAsync(string ticker, int giorni = 300);
    }
}
