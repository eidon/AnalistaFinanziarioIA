namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class PortafoglioDashboardDTO
    {
        public decimal ValoreTotale { get; set; }
        public decimal ProfittoTotale { get; set; }
        public decimal ProfittoPercentuale { get; set; }
        public List<AssetPerformanceDTO> Assets { get; set; } = [];
    }
}