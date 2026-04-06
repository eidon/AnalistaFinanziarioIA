namespace AnalistaFinanziarioIA.Core.Models;

public class QuotazioneStorica
{
    public int Id { get; set; }
    public int TitoloId { get; set; }
    public DateTime Data { get; set; }
    public decimal PrezzoApertura { get; set; }
    public decimal PrezzoChiusura { get; set; }
    public decimal PrezzoMassimo { get; set; }
    public decimal PrezzoMinimo { get; set; }
    public long Volume { get; set; }

    public Titolo Titolo { get; set; } = null!;
}
