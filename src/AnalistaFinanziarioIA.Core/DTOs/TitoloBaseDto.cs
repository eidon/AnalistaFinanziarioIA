using AnalistaFinanziarioIA.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public abstract class TitoloBaseDto
    {
        public string Simbolo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;

        // Usiamo l'enumeratore definito nei Models
        public TipoTitolo Tipo { get; set; }

        public string Valuta { get; set; } = "EUR";
        public string? Isin { get; set; }
    }
}
