namespace AnalistaFinanziarioIA.Core.Models
{
    public class Dividendo
    {
        public int Id { get; set; }
        public int AssetPortafoglioId { get; set; }
        public decimal ImportoLordo { get; set; }
        public decimal TasseTrattenute { get; set; }
        // Valuta del dividendo (potrebbe differire dalla valuta del titolo)
        public string Valuta { get; set; } = "EUR";
        public DateTime DataPagamento { get; set; }
    }
}