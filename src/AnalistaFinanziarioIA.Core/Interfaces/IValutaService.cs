namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface IValutaService
{
    decimal GetTassoCambio(string da, string a);
    decimal ConvertiInEur(decimal importo, string valutaOriginale);
}
