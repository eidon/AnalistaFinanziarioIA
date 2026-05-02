using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.API.BackgroundServices
{
    public class PrezzoAggiornamentoService(IServiceProvider _services, ILogger<PrezzoAggiornamentoService> _logger) : BackgroundService
    {
        private readonly TimeSpan _periodo = TimeSpan.FromMinutes(15);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Dispose the scope before the delay to release DbContext and other resources
                using (var scope = _services.CreateScope())
                {
                    var titoloRepo = scope.ServiceProvider.GetRequiredService<ITitoloRepository>();
                    var quotazioneRepo = scope.ServiceProvider.GetRequiredService<IQuotazioneRepository>();
                    var yahooFinance = scope.ServiceProvider.GetRequiredService<IYahooFinanceService>();

                    var titoli = await titoloRepo.GetAllAsync();

                    // 1. Avvio parallelo delle chiamate
                    var fetchTasks = titoli.Select(async t => {
                        try
                        {
                            var prezzo = await yahooFinance.GetQuoteAsync(t.Simbolo);
                            return (Id: t.Id, Simbolo: t.Simbolo, Prezzo: prezzo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Errore durante il fetch per {Simbolo}", t.Simbolo);
                            return (Id: t.Id, Simbolo: t.Simbolo, Prezzo: 0m); // Ritorna 0 per saltarlo nel foreach
                        }
                    }).ToList(); // .ToList() assicura l'avvio immediato dei Task

                    // 2. Attesa collettiva
                    var risultati = await Task.WhenAll(fetchTasks);

                    // 3. Salvataggio sequenziale
                    foreach (var res in risultati)
                    {
                        if (res.Prezzo > 0)
                        {
                            await quotazioneRepo.SalvaQuotazioneAsync(res.Id, res.Prezzo);
                            _logger.LogDebug("Prezzo aggiornato per {Simbolo}: {Prezzo}", res.Simbolo, res.Prezzo);
                        }
                    }

                }

                await Task.Delay(_periodo, stoppingToken);
            }
        }
    }
}
