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

            // Trasformiamo il prezzo unitario in Euro usando il tasso cristallizzato sulla transazione
            // Se la transazione è già in EUR, il tasso sarà 1.0
            decimal prezzoUnitarioEur = t.PrezzoUnitario * t.TassoCambio;
            decimal tasseEur = t.Tasse * t.TassoCambio;

            if (t.TipoOperazione == TipoTransazione.Acquisto)
            {
                decimal costoTotalePrecedente = QuantitaTotale * PrezzoMedioCarico;
                decimal costoNuovoAcquisto = (t.Quantita * prezzoUnitarioEur) + t.Commissioni + tasseEur;

                QuantitaTotale += t.Quantita;
                if (QuantitaTotale > 0)
                {
                    PrezzoMedioCarico = (costoTotalePrecedente + costoNuovoAcquisto) / QuantitaTotale;
                }
            }
            else if (t.TipoOperazione == TipoTransazione.Vendita)
            {
                decimal incassoNettoVendita = (t.Quantita * prezzoUnitarioEur) - t.Commissioni - tasseEur;
                decimal valoreCaricoVenduto = t.Quantita * PrezzoMedioCarico;

                ProfittoRealizzatoTotale += (incassoNettoVendita - valoreCaricoVenduto);
                QuantitaTotale -= t.Quantita;

                if (QuantitaTotale <= 0)
                {
                    QuantitaTotale = 0;
                    PrezzoMedioCarico = 0;
                }
            }

        }
    
    
    }
}