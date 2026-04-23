using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace AnalistaFinanziarioIA.API.Services;

// ─────────────────────────────────────────────────────────────────────────────
// INTERFACCIA
// In produzione: implementare con IDistributedCache (Redis) per scalabilità
// orizzontale e persistenza tra restart del server.
// ─────────────────────────────────────────────────────────────────────────────

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

// ─────────────────────────────────────────────────────────────────────────────
// IMPLEMENTAZIONE IN-MEMORY
// Thread-safe grazie a ConcurrentDictionary e SemaphoreSlim per-utente.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class InMemoryChatSessionService : IChatSessionService
{
    private record SessioneChat(ChatHistory History, DateTime UltimoAccesso);

    private readonly ConcurrentDictionary<Guid, SessioneChat> _sessioni = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    // ── Lock per-utente ──────────────────────────────────────────────────────

    public async Task AcquireLockAsync(Guid utenteId, CancellationToken ct = default)
    {
        var sem = _locks.GetOrAdd(utenteId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
    }

    public void ReleaseLock(Guid utenteId)
    {
        if (_locks.TryGetValue(utenteId, out var sem))
            sem.Release();
    }

    // ── Gestione History ─────────────────────────────────────────────────────

    public Task<ChatHistory> GetOrCreateHistoryAsync(Guid utenteId, Func<ChatHistory> factory)
    {
        if (_sessioni.TryGetValue(utenteId, out var sessione))
            return Task.FromResult(sessione.History);

        var nuova = factory();
        _sessioni[utenteId] = new SessioneChat(nuova, DateTime.UtcNow);
        return Task.FromResult(nuova);
    }

    public Task SaveHistoryAsync(Guid utenteId, ChatHistory history)
    {
        _sessioni[utenteId] = new SessioneChat(history, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    // ── Cleanup sessioni scadute ─────────────────────────────────────────────

    public void PulisciSessioniScadute(TimeSpan ttl)
    {
        var soglia = DateTime.UtcNow - ttl;

        foreach (var (key, sessione) in _sessioni)
        {
            if (sessione.UltimoAccesso < soglia)
            {
                _sessioni.TryRemove(key, out _);

                // Disposa il semaphore solo se non è in uso
                if (_locks.TryRemove(key, out var sem) && sem.CurrentCount == 1)
                    sem.Dispose();
            }
        }
    }
}