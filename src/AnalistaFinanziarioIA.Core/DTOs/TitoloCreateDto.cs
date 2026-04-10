using System.ComponentModel.DataAnnotations;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class TitoloCreateDto
    {
        [Required(ErrorMessage = "Il Simbolo (Ticker) è obbligatorio")]
        [MaxLength(10)]
        public string Simbolo { get; set; } = null!; // es: AAPL, STLA

        [Required(ErrorMessage = "Il nome dell'azienda è obbligatorio")]
        [MaxLength(100)]
        public string Nome { get; set; } = null!; // es: Apple Inc.

        [Required]
        public string Isin { get; set; } = string.Empty;

        [Required]
        public string Valuta { get; set; } = "EUR"; // Default a EUR

        public string? Mercato { get; set; } // es: NYSE, Borsa Italiana

        public string? Settore { get; set; } // Fondamentale per l'analisi della diversificazione dell'IA
    }
}
