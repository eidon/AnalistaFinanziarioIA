using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.Core.Services
{
    public class PortafoglioService(
        IPortafoglioRepository _repository,
        IValutaService _valutaService)
    {
        public async Task<object> GetDashboardCompletaAsync(Guid utenteId)
        {
            var posizioni = await _repository.GetDashboardAssetsAsync(utenteId);

            decimal totaleValoreEur = 0;
            decimal totaleGuadagnoEur = 0;

            foreach (var p in posizioni)
            {
                // Qui usiamo il tuo ValutaService per normalizzare tutto in EUR
                totaleValoreEur += _valutaService.ConvertiInEur(p.ValoreDiMercato, p.Valuta);
                totaleGuadagnoEur += _valutaService.ConvertiInEur(p.GuadagnoAssoluto, p.Valuta);
            }

            // Calcolo della percentuale totale sul portafoglio
            decimal capitaleInvestitoEur = totaleValoreEur - totaleGuadagnoEur;
            decimal percentualeTotale = capitaleInvestitoEur > 0
                ? (totaleGuadagnoEur / capitaleInvestitoEur) * 100
                : 0;

            return new
            {
                TotalePortafoglioEur = totaleValoreEur,
                ProfittoTotaleEur = totaleGuadagnoEur,
                PercentualeTotale = percentualeTotale,
                Assets = posizioni
            };
        }


    }
}