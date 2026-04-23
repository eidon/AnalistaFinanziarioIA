using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.Core.Services
{
    public class PortafoglioService(
        IPortafoglioRepository _repository,
        IValutaService _valutaService)
    {
        public async Task<object> GetDashboardCompletaAsync(Guid utenteId)
        {
            // 1. Recuperiamo TUTTE le posizioni (comprese quelle chiuse con Quantita = 0)
            var tutteLePosizioni = await _repository.GetDashboardAssetsAsync(utenteId);

            // 2. Recuperiamo i costi vivi (Commissioni e Tasse)
            var costiTransazioni = await _repository.GetTotaleCostiTransazioniAsync(utenteId);

            decimal totaleValoreAttualeEur = 0;
            decimal totaleGuadagnoLatenteEur = 0;   // Guadagno Prezzo (Asset aperti)
            decimal totaleGuadagnoRealizzatoEur = 0; // Guadagno Consolidato (Vendite passate)
            decimal capitaleEffettivamenteSborsatoEur = 0;

            foreach (var p in tutteLePosizioni)
            {
                // Guadagno Realizzato: lo sommiamo sempre (anche se la posizione è chiusa)
                totaleGuadagnoRealizzatoEur += _valutaService.ConvertiInEur(p.ProfittoRealizzatoTotale, p.Valuta);

                // Se la posizione è aperta, calcoliamo i dati per il portafoglio attuale
                if (p.Quantita > 0)
                {
                    totaleValoreAttualeEur += _valutaService.ConvertiInEur(p.ValoreDiMercato, p.Valuta);
                    totaleGuadagnoLatenteEur += _valutaService.ConvertiInEur(p.GuadagnoAssoluto, p.Valuta);

                    // Il capitale investito è: Quantità * Prezzo Medio di Carico
                    decimal costoCaricoOriginario = p.Quantita * p.Pmc;
                    capitaleEffettivamenteSborsatoEur += _valutaService.ConvertiInEur(costoCaricoOriginario, p.Valuta);
                }
            }

            // Il profitto totale è: (Cosa guadagnerei vendendo tutto oggi) + (Cosa ho già guadagnato vendendo prima)
            decimal profittoTotaleLordoEur = totaleGuadagnoLatenteEur + totaleGuadagnoRealizzatoEur;

            // Al netto dei costi (Commissioni e Tasse)
            decimal totaleCostiEur = costiTransazioni.Commissioni + costiTransazioni.Tasse;
            decimal rendimentoNettoEur = profittoTotaleLordoEur - totaleCostiEur;

            // Filtriamo per la tabella (solo titoli con quantità > 0)
            var assetsDaVisualizzare = tutteLePosizioni
                .Where(p => p.Quantita > 0)
                .ToList();

            return new
            {
                TotalePortafoglioEur = totaleValoreAttualeEur,
                ProfittoTotaleEur = rendimentoNettoEur, // Questo va nelle card in alto
                PercentualeTotale = capitaleEffettivamenteSborsatoEur > 0
                    ? (rendimentoNettoEur / capitaleEffettivamenteSborsatoEur) * 100
                    : 0,

                Statistiche = new
                {
                    CapitaleInvestito = capitaleEffettivamenteSborsatoEur,
                    GuadagnoPrezzo = totaleGuadagnoLatenteEur,
                    GuadagnoRealizzato = totaleGuadagnoRealizzatoEur,
                    CostiTotali = totaleCostiEur
                },

                Assets = assetsDaVisualizzare
            };
        }

    }

}