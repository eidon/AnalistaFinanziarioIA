using System.ComponentModel.DataAnnotations;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class AnalisiCreateDto
    {
        [Required(ErrorMessage = "Il risultato dell'analisi è obbligatorio")]
        [MinLength(10, ErrorMessage = "L'analisi deve essere più dettagliata")]
        public string Risultato { get; set; } = string.Empty;

        [Range(-100, 10000)]
        public decimal PrezzoTarget { get; set; }

        [Required]
        public string Raccomandazione { get; set; } = "Hold"; // Buy, Sell, Hold

        public string? Note { get; set; }
    }
}
