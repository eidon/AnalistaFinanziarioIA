using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class RegistraTransazioneDto
    {
        public Guid UtenteId { get; set; }
        public int? TitoloId { get; set; }

        // Questo mappa 'titoloLookup' del tuo JS
        public TitoloLookupDto? TitoloLookup { get; set; }

        public decimal Quantita { get; set; }
        public decimal PrezzoUnita { get; set; } // Nome corretto dal tuo JS
        public decimal Commissioni { get; set; }
        public decimal Tasse { get; set; }
        public string? Note { get; set; }
        public DateTime DataOperazione { get; set; }

        // Se vuoi gestire Acquisto/Vendita dalla modale, aggiungi anche questo
        public string TipoOperazione { get; set; } = "Acquisto";
    }
}
