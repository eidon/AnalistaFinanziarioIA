namespace AnalistaFinanziarioIA.Core.DTOs;

public class TitoloLookupDto
{
    // L'Id sarà valorizzato solo se il titolo è già nel tuo DB
    public int? Id { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Valuta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty; // Azione, ETF, ecc.
    public string Regione { get; set; } = string.Empty;

    // Flag per far capire al frontend se il titolo è nuovo o già censito
    public bool GiaPresenteNelDb => Id.HasValue;
}
