using System.ComponentModel.DataAnnotations;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class TransazioneInputDto
    {
        [Required]
        public Guid UtenteId { get; set; }
        
        public int? TitoloId { get; set; } // Nullable: se è null, usiamo TitoloLookup

        public TitoloLookupDto? TitoloLookup { get; set; } // I dati da Alpha Vantage

        [Range(0.00001, double.MaxValue, ErrorMessage = "La quantità deve essere maggiore di zero")]
        public decimal Quantita { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Il prezzo deve essere maggiore di zero")]
        public decimal PrezzoUnitario { get; set; }

        public decimal Commissioni { get; set; }

        public decimal Tasse { get; set; }

        public string? Note { get; set; }

        // Opzionale: se vuoi permettere all'utente di scegliere la data, 
        // altrimenti la decide il server come DateTime.UtcNow
        public DateTime? Data { get; set; }
    }
}
