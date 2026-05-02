namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IValutaService
    {
        Task<decimal> GetTassoCambioAsync(string da, string a = "EUR");
        Task<decimal> ConvertiInEurAsync(decimal importo, string valutaOriginale);
    }
}