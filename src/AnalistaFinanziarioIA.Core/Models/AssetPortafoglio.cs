namespace AnalistaFinanziarioIA.Core.Models
{
    public class AssetPortafoglio
    {
        public int Id { get; set; }
        public Guid UtenteId { get; set; }

        // FK verso il Titolo esistente (es. "AAPL")
        public int TitoloId { get; set; }
        public Titolo? Titolo { get; set; }

        public decimal QuantitaTotale { get; set; }
        // Prezzo medio in valuta originale del titolo
        public decimal PrezzoMedioCarico { get; set; }

        public decimal ProfittoRealizzatoTotale { get; set; } = 0;
        // Relazione: un asset ha molte transazioni e dividendi
        public ICollection<Transazione> Transazioni { get; set; } = new List<Transazione>();
        public ICollection<Dividendo> Dividendi { get; set; } = new List<Dividendo>();
    }
}