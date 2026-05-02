using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;

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
            var (Commissioni, Tasse) = await _repository.GetTotaleCostiTransazioniAsync(utenteId);

            var assetsPerUi = new List<AssetDisplayDto>();

            decimal totaleValoreAttualeEur = 0;
            decimal totaleGuadagnoLatenteEur = 0;
            decimal totaleGuadagnoRealizzatoEur = 0;
            decimal capitaleAttualeInvestitoEur = 0;

            foreach (var asset in tutteLePosizioni)
            {
                // 1. Ricalcolo Totale Pezzi e PMC reale dalle transazioni
                decimal qCalcolata = 0;
                decimal pmcCalcolatoEur = 0;
                decimal realizzatoAssetEur = 0;

                // Ordiniamo per data per ricostruire la storia correttamente (essenziale per Nike!)
                var transazioniOrdinate = asset.Transazioni.OrderBy(x => x.Data).ToList();

                foreach (var t in transazioniOrdinate)
                {
                    // Convertiamo il prezzo al cambio del giorno della transazione
                    decimal prezzoEur = await _valutaService.ConvertiInEurAsync(t.PrezzoUnitario, asset.Titolo?.Valuta ?? "EUR");

                    if (t.TipoOperazione == TipoTransazione.Acquisto)
                    {
                        pmcCalcolatoEur = ((qCalcolata * pmcCalcolatoEur) + (t.Quantita * prezzoEur)) / (qCalcolata + t.Quantita);
                        qCalcolata += t.Quantita;
                    }
                    else // Vendita
                    {
                        realizzatoAssetEur += t.Quantita * (prezzoEur - pmcCalcolatoEur);
                        qCalcolata -= t.Quantita;
                    }
                }

                // 2. Prezzo attuale convertito oggi
                decimal prezzoAttualeEur = await _valutaService.ConvertiInEurAsync(asset.Titolo.UltimoPrezzo, asset.Titolo.Valuta);

                // 3. Creazione del DTO (usando i dati ricalcolati)
                var dto = new AssetDisplayDto
                {
                    AssetId = asset.Id,
                    Nome = asset.Titolo.Nome,
                    Simbolo = asset.Titolo.Simbolo,
                    Quantita = qCalcolata, // Qui Nike sarà 85
                    Pmc = pmcCalcolatoEur,
                    PrezzoAttuale = prezzoAttualeEur,
                    Valuta = "EUR",
                    ProfittoRealizzatoTotale = realizzatoAssetEur
                };

                totaleGuadagnoRealizzatoEur += realizzatoAssetEur;

                if (qCalcolata > 0)
                {
                    totaleValoreAttualeEur += dto.ValoreDiMercato;
                    totaleGuadagnoLatenteEur += dto.GuadagnoAssoluto;
                    capitaleAttualeInvestitoEur += dto.InvestitoTotale;
                    assetsPerUi.Add(dto);
                }
            }

            decimal totaleCostiEur = Commissioni + Tasse;

            decimal profittoTotaleNetto = (totaleGuadagnoLatenteEur + totaleGuadagnoRealizzatoEur) - totaleCostiEur;

            return new
            {
                TotalePortafoglioEur = totaleValoreAttualeEur,
                ProfittoTotaleEur = profittoTotaleNetto,
                PercentualeTotale = capitaleAttualeInvestitoEur > 0 ? (profittoTotaleNetto / capitaleAttualeInvestitoEur) * 100 : 0,
                Assets = assetsPerUi,
                Statistiche = new
                {
                    CapitaleInvestito = capitaleAttualeInvestitoEur,
                    GuadagnoPrezzo = totaleGuadagnoLatenteEur,
                    GuadagnoRealizzato = totaleGuadagnoRealizzatoEur,
                    CostiTotali = totaleCostiEur
                }
            };
        }

        public async Task<IEnumerable<StoriaTransazioniDto>> GetStoriaRaggruppataAsync(Guid utenteId, string? search)
        {
            // 1. Recuperiamo la lista già filtrata e tipizzata
            var transazioni = await _repository.GetStoriaFiltrataAsync(utenteId, search);

            // 2. Raggruppiamo e mappiamo
            return transazioni
                .GroupBy(t => {
                    // Gestiamo il null qui per il raggruppamento
                    var d = t.Data ?? DateTime.MinValue;
                    return new { d.Year, d.Month };
                })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .Select(g => new StoriaTransazioniDto
                {
                    Periodo = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    Movimenti = g.Select(t => {
                        // Risolviamo l'errore CS1501: estraiamo il DateTime reale o usiamo MinValue
                        var dataReale = t.Data ?? DateTime.MinValue;

                        return new MovimentoDto
                        {
                            Id = t.Id,
                            // Ora ToString("dd.MM") funziona perché dataReale NON è nullable
                            GiornoMese = dataReale.ToString("dd.MM"),
                            Nome = t.Nome,
                            Simbolo = t.Simbolo,
                            Tipo = t.TipoOperazione.ToString(),
                            Descrizione = $"{(t.TipoOperazione == TipoTransazione.Acquisto ? "Acquistato" : "Venduto")} x{t.Quantita:N0} a {t.PrezzoUnitario:N2} {t.Valuta}",
                            TotaleOperazione = t.Quantita * t.PrezzoUnitario,
                            Valuta = t.Valuta,
                            Icona = t.TipoOperazione == TipoTransazione.Acquisto ? "in" : "out"
                        };
                    }).ToList()
                }).ToList();
        }

        public async Task<IEnumerable<object>> GetAssetsFiltratiAsync(Guid utenteId, string? query)
        {
            var assets = await _repository.GetAssetsAttiviAsync(utenteId);

            return assets
                .Where(a => string.IsNullOrEmpty(query) ||
                            a.Titolo!.Nome.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            a.Titolo!.Simbolo.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(a => new {
                    Id = a.TitoloId, // ID reale del titolo per future operazioni
                    Simbolo = a.Titolo!.Simbolo,
                    Nome = a.Titolo.Nome,
                    Quantita = a.QuantitaTotale,
                    Valuta = a.Titolo.Valuta,
                    PrezzoMedio = a.PrezzoMedioCarico
                })
                .ToList();
        }

    }

}