namespace AnalistaFinanziarioIA.Core.Models;

public class Titolo
{
    public int Id { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Settore { get; set; } = string.Empty;
    public string Mercato { get; set; } = string.Empty;
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

    public ICollection<QuotazioneStorica> QuotazioniStoriche { get; set; } = new List<QuotazioneStorica>();
    public ICollection<AnalisiFinanziaria> Analisi { get; set; } = new List<AnalisiFinanziaria>();
}
