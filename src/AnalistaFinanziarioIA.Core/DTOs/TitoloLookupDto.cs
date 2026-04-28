using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.DTOs;

public class TitoloLookupDto : TitoloBaseDto
{
    // L'Id sarà valorizzato solo se il titolo è già nel tuo DB
    public int? Id { get; set; }

    public string Mercato { get; set; } = string.Empty;
    public string Settore { get; set; } = string.Empty;
    public decimal PrezzoAttuale { get; set; }

    // Flag per far capire al frontend se il titolo è nuovo o già censito
    public bool GiaPresenteNelDb => Id.HasValue;

}
