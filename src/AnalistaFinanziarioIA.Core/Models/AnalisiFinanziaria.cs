namespace AnalistaFinanziarioIA.Core.Models;

public class AnalisiFinanziaria
{
    public int Id { get; set; }
    public int TitoloId { get; set; }
    public DateTime DataAnalisi { get; set; } = DateTime.UtcNow;
    public string TipoAnalisi { get; set; } = string.Empty;
    public string Risultato { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal? PrezzoTarget { get; set; }
    public string Raccomandazione { get; set; } = string.Empty;

    public Titolo Titolo { get; set; } = null!;
}
