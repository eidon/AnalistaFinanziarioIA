using AnalistaFinanziarioIA.API.Services;

namespace AnalistaFinanziarioIA.API.BackgroundServices;

/// <summary>
/// Servizio in background che pulisce periodicamente le sessioni chat scadute.
/// Intervallo di cleanup: ogni 30 minuti.
/// TTL sessione: 2 ore di inattività.
/// </summary>
public sealed class SessionCleanupService(
    IChatSessionService sessionService,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan IntervalloCleanup = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan TtlSessione = TimeSpan.FromHours(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SessionCleanupService avviato. Cleanup ogni {Intervallo} min, TTL {TTL} ore.",
            IntervalloCleanup.TotalMinutes, TtlSessione.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(IntervalloCleanup, stoppingToken);

            try
            {
                sessionService.PulisciSessioniScadute(TtlSessione);
                logger.LogDebug("Cleanup sessioni completato alle {Ora}.", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante il cleanup delle sessioni chat.");
            }
        }
    }
}