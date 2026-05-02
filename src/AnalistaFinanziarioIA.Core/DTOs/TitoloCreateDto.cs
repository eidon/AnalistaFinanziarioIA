using System.ComponentModel.DataAnnotations;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class TitoloCreateDto : TitoloBaseDto
    {
        [Required(ErrorMessage = "Il Simbolo (Ticker) è obbligatorio")]
        [MaxLength(10)]
        public new string Simbolo { get; set; } = null!; // es: AAPL, STLA

        [Required(ErrorMessage = "Il nome dell'azienda è obbligatorio")]
        [MaxLength(100)]
        public new string Nome { get; set; } = null!; // es: Apple Inc.

        [Required]
        public new string Isin { get; set; } = string.Empty;

        [Required]
        public new string Valuta { get; set; } = "EUR"; // Default a EUR

        // Campi aggiuntivi specifici che servono al momento del salvataggio nel DB
        public string? Mercato { get; set; } // es: NYSE, Borsa Italiana

        public string? Settore { get; set; } // Fondamentale per l'analisi della diversificazione dell'IA
    }
}
