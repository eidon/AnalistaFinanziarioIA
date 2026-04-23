
namespace AnalistaFinanziarioIA.Core.Services
{
    public interface IValutaService
    {
        decimal GetTassoCambio(string da, string a);
        decimal ConvertiInEur(decimal importo, string valutaOriginale);
    }

    public class ValutaService : IValutaService
    {
        public decimal GetTassoCambio(string da, string a)
        {
            if (da == a) return 1.0m;
            if (da == "USD" && a == "EUR") return 0.85m; // Esempio: 1 USD = 0.92 EUR
            if (da == "EUR" && a == "USD") return 1.09m; // Esempio: 1 EUR = 1.09 USD
            return 1.0m;
        }

        public decimal ConvertiInEur(decimal importo, string valutaOriginale)
        {
            return importo * GetTassoCambio(valutaOriginale, "EUR");
        }
    }
}