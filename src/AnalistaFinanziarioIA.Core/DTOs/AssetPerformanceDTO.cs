namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class AssetPerformanceDTO
    {
        public int TitoloId { get; set; }
        public decimal QuantitaPosseduta { get; set; }
        public decimal PrezzoMedioCarico { get; set; }
        public decimal PrezzoCorrente { get; set; }
        public decimal ValoreAttuale { get; set; }
        public decimal Profitto { get; set; }
    }
}