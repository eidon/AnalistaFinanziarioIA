namespace AnalistaFinanziarioIA.Core.Models
{
    public class AssetPortafoglio
    {
        public int Id { get; set; }
        public Guid UtenteId { get; set; }
        public int TitoloId { get; set; }
        public Titolo? Titolo { get; set; }
        public decimal QuantitaTotale { get; set; }
        public decimal PrezzoMedioCarico { get; set; }
        public decimal ProfittoRealizzatoTotale { get; set; } = 0;

        public ICollection<Transazione> Transazioni { get; set; } = new List<Transazione>();
        public ICollection<Dividendo> Dividendi { get; set; } = new List<Dividendo>();

        /// <summary>
        /// Logica di Business: Aggiorna lo stato dell'asset in base a una nuova operazione.
        /// </summary>
        public void ApplicaTransazione(Transazione t)
        {
            if (t.TipoOperazione == TipoTransazione.Acquisto)
            {
                // Calcolo del nuovo PMC (Prezzo Medio di Carico)
                decimal costoTotalePrecedente = QuantitaTotale * PrezzoMedioCarico;

                // Il costo dell'acquisto include commissioni e tasse
                decimal costoNuovoAcquisto = (t.Quantita * t.PrezzoUnitario) + t.Commissioni + t.Tasse;

                QuantitaTotale += t.Quantita;

                if (QuantitaTotale > 0)
                {
                    PrezzoMedioCarico = (costoTotalePrecedente + costoNuovoAcquisto) / QuantitaTotale;
                }
            }
            else if (t.TipoOperazione == TipoTransazione.Vendita)
            {
                // Gestione Profitto/Perdita Realizzata (P&L)
                // Si calcola sulla differenza tra prezzo di vendita (netto) e PMC attuale
                decimal incassoNettoVendita = (t.Quantita * t.PrezzoUnitario) - t.Commissioni - t.Tasse;
                decimal valoreCaricoVenduto = t.Quantita * PrezzoMedioCarico;

                ProfittoRealizzatoTotale += (incassoNettoVendita - valoreCaricoVenduto);

                // Diminuzione quantità
                QuantitaTotale -= t.Quantita;

                // Se la posizione si chiude, azzeriamo il PMC per pulizia
                if (QuantitaTotale <= 0)
                {
                    QuantitaTotale = 0;
                    PrezzoMedioCarico = 0;
                }
            }
        }
    }
}