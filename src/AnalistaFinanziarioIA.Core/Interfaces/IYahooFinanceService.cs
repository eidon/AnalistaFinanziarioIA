namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IYahooFinanceService
    {
        /// <summary>
        /// Recupera il prezzo attuale per un determinato ticker.
        /// </summary>
        Task<decimal> GetQuoteAsync(string simbolo);
    }
}
