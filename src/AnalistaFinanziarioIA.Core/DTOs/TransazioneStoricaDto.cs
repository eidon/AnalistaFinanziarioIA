namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class TransazioneStoricaDto : TransazioneInputDto
    {
        public int Id { get; set; }        // Identificativo univoco della transazione
        public string Nome { get; set; } = string.Empty;    // Nome del titolo (es. Nike, Inc.)
        public string Simbolo { get; set; } = string.Empty;  // Simbolo/Ticker (es. NKE)
        public string Valuta { get; set; } = "EUR";
    }
}
