namespace AnalistaFinanziarioIA.Core.Models;

public enum TipoTitolo { Azione, ETF, BTP, Crypto, ObbligazioneCorp }

public class Titolo
{
    public int Id { get; set; }
    public string Simbolo { get; set; } = string.Empty; // Es: VWCE
    public string? Isin { get; set; }                   // Es: IE00BK5BQT80
    public string Nome { get; set; } = string.Empty;
    public string? Settore { get; set; }
    public string? Mercato { get; set; }                // Es: Borsa Italiana, XETRA
    public string Valuta { get; set; } = "EUR";         // Fondamentale per i calcoli!
    public DateTime DataCreazione { get; set; } = DateTime.Now;
    public TipoTitolo Categoria { get; set; }

    public decimal UltimoPrezzo { get; set; }

    public ICollection<QuotazioneStorica> QuotazioniStoriche { get; set; } = new List<QuotazioneStorica>();
    public ICollection<AnalisiFinanziaria> Analisi { get; set; } = new List<AnalisiFinanziaria>();
}
