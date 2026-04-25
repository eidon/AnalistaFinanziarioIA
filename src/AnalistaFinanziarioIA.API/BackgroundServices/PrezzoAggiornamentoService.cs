using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.API.BackgroundServices
{
    public class PrezzoAggiornamentoService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PrezzoAggiornamentoService> _logger;
        private readonly TimeSpan _periodo = TimeSpan.FromMinutes(15);

        public PrezzoAggiornamentoService(IServiceProvider services, ILogger<PrezzoAggiornamentoService> logger)
        {
            _services = services;
            _logger = logger;
        }

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

                    foreach (var titolo in titoli)
                    {
                        var nuovoPrezzo = await yahooFinance.GetQuoteAsync(titolo.Simbolo);

                        if (nuovoPrezzo > 0)
                        {
                            await quotazioneRepo.SalvaQuotazioneAsync(titolo.Id, nuovoPrezzo);
                            _logger.LogDebug("Prezzo aggiornato per {Simbolo}: {Prezzo}", titolo.Simbolo, nuovoPrezzo);
                        }
                    }
                }

                await Task.Delay(_periodo, stoppingToken);
            }
        }
    }
}
