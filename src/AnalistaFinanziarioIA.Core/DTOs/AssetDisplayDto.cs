namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class AssetDisplayDto
    {
        public int AssetId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Simbolo { get; set; } = string.Empty;
        public decimal Quantita { get; set; }
        public decimal PrezzoAttuale { get; set; } // Recuperato da API o database
        public decimal Pmc { get; set; }           // Calcolato dinamicamente
        public string Valuta { get; set; } = "EUR";

        // Proprietà calcolate per la UI
        public decimal ValoreDiMercato => Quantita * PrezzoAttuale;
        public decimal InvestitoTotale => Quantita * Pmc;
        public decimal GuadagnoAssoluto => ValoreDiMercato - InvestitoTotale;
        public decimal GuadagnoPercentuale => Pmc > 0 ? ((PrezzoAttuale - Pmc) / Pmc) * 100 : 0;

        public decimal ProfittoRealizzatoTotale { get; set; }
    }
}