using Microsoft.SemanticKernel.ChatCompletion;

namespace AnalistaFinanziarioIA.API.Services;

// In produzione: implementare con IDistributedCache (Redis) per scalabilità
// orizzontale e persistenza tra restart del server.
public interface IChatSessionService
{
    /// <summary>Acquisisce il lock esclusivo per l'utente (previene race condition).</summary>
    Task AcquireLockAsync(Guid utenteId, CancellationToken ct = default);

    /// <summary>Rilascia il lock dell'utente.</summary>
    void ReleaseLock(Guid utenteId);

    /// <summary>Restituisce la ChatHistory esistente o ne crea una nuova tramite la factory.</summary>
    Task<ChatHistory> GetOrCreateHistoryAsync(Guid utenteId, Func<ChatHistory> factory);

    /// <summary>Persiste la ChatHistory aggiornata e aggiorna il timestamp di accesso.</summary>
    Task SaveHistoryAsync(Guid utenteId, ChatHistory history);

    /// <summary>Elimina tutte le sessioni non aggiornate da più di <paramref name="ttl"/>.</summary>
    void PulisciSessioniScadute(TimeSpan ttl);
}
