namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class AnalisiReadDto
    {
        public int Id { get; set; }
        public string Risultato { get; set; }
        public decimal PrezzoTarget { get; set; }
        public string Raccomandazione { get; set; }
        public DateTime DataAnalisi { get; set; }
        public string TickerTitolo { get; set; } // Comodo per la UI
    }
}
