namespace AnalistaFinanziarioIA.Core.DTOs
{
    // Questo rappresenta la singola riga nella lista
    public class MovimentoDto
    {
        public int Id { get; set; }
        public string GiornoMese { get; set; } = string.Empty; // "10.04"
        public string TitoloNome { get; set; } = string.Empty; // "Leonardo"
        public string Ticker { get; set; } = string.Empty;     // "LDO"
        public string Tipo { get; set; } = string.Empty;       // "Acquisto" o "Vendita"
        public string Descrizione { get; set; } = string.Empty; // "Acquistato x35 a 56,20 €"
        public decimal TotaleOperazione { get; set; }
        public string Valuta { get; set; } = string.Empty;
        public string Icona { get; set; } = string.Empty;      // "in" o "out" (o arrow_forward/back)
    }

    // Questo è l'oggetto che raggruppa per mese
    public class StoriaTransazioniDto
    {
        public string Periodo { get; set; } = string.Empty; // "aprile 2026"
        public List<MovimentoDto> Movimenti { get; set; } = [];
    }
}