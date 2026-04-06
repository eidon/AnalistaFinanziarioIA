namespace AnalistaFinanziarioIA.Core.Models
{
    public enum TipoTransazione { Acquisto, Vendita, Split }

    public class Transazione
    {
        public int Id { get; set; }
        public int AssetPortafoglioId { get; set; }
        public TipoTransazione Tipo { get; set; }
        public decimal Quantita { get; set; }
        public decimal PrezzoUnita { get; set; } // In valuta originale
        public decimal Commissioni { get; set; }
        // Tasso di cambio al momento della transazione (es. EUR/USD)
        public decimal TassoCambio { get; set; }
        public DateTime Data { get; set; } = DateTime.UtcNow;
    }
}