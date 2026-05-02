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

            // Convertiamo tutto in EUR usando il TassoCambio della transazione
            // Se la transazione è già in EUR, il tasso sarà 1.0
            decimal prezzoUnitarioEur = t.PrezzoUnitario * t.TassoCambio;
            decimal commissioniEur = t.Commissioni * t.TassoCambio;

            if (t.TipoOperazione == TipoTransazione.Acquisto)
            {
                // ACQUISTO: Le commissioni si AGGIUNGONO al costo
                decimal costoTotaleNuovo = (t.Quantita * prezzoUnitarioEur) + commissioniEur;
                decimal costoTotalePrecedente = QuantitaTotale * PrezzoMedioCarico;

                QuantitaTotale += t.Quantita;
                // Nuovo PMC che include i costi di transazione
                PrezzoMedioCarico = (costoTotalePrecedente + costoTotaleNuovo) / QuantitaTotale;
            }
            else if (t.TipoOperazione == TipoTransazione.Vendita)
            {
                // VENDITA: Le commissioni si SOTTRAGGONO all'incasso
                decimal incassoNettoEur = (t.Quantita * prezzoUnitarioEur) - commissioniEur;
                decimal valoreCaricoEur = t.Quantita * PrezzoMedioCarico;

                // Il profitto calcolato è reale: quanto ho incassato pulito - quanto l'avevo pagato
                decimal profittoDiQuestaVendita = incassoNettoEur - valoreCaricoEur;

                ProfittoRealizzatoTotale += profittoDiQuestaVendita;
                QuantitaTotale -= t.Quantita;

                // Nota: Il PMC non cambia mai durante una vendita
            }

        }
    
    
    }
}