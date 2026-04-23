
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.API.BackgroundServices
{
    public class PrezzoAggiornamentoService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PrezzoAggiornamentoService> _logger;
        private readonly TimeSpan _periodo = TimeSpan.FromMinutes(15); // Aggiorna ogni 15 min

        public PrezzoAggiornamentoService(IServiceProvider services, ILogger<PrezzoAggiornamentoService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AnalistaFinanziarioDbContext>();
                    var yahooFinance = scope.ServiceProvider.GetRequiredService<IYahooFinanceService>();

                    // 1. Prendi tutti i ticker distinti presenti nel DB
                    var tickers = await context.Titoli.Select(t => t.Simbolo).ToListAsync();

                    foreach (var ticker in tickers)
                    {
                        // 2. Recupera il prezzo reale (es. da Yahoo)
                        var nuovoPrezzo = await yahooFinance.GetQuoteAsync(ticker);

                        if (nuovoPrezzo > 0)
                        {
                            // 3. Aggiorna tutti i titoli con quel ticker
                            var titoliDaAggiornare = context.Titoli.Where(t => t.Simbolo == ticker);
                            foreach (var t in titoliDaAggiornare)
                            {
                                t.UltimoPrezzo = nuovoPrezzo;
                                t.DataUltimoPrezzo = DateTime.UtcNow;
                            }
                        }
                    }
                    await context.SaveChangesAsync();
                }
                await Task.Delay(_periodo, stoppingToken);
            }
        }
    }
}
