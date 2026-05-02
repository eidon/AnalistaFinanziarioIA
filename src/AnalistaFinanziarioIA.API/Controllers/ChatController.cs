using AnalistaFinanziarioIA.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AnalistaFinanziarioIA.API.Controllers;

/// <summary>
/// Controller per la chat con l'Analista Finanziario IA.
/// Il Kernel è Scoped: ogni request ottiene la propria istanza con i plugin già registrati in DI.
/// La gestione delle sessioni è delegata a IChatSessionService (thread-safe, con TTL).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController(
    Kernel kernel,
    IChatSessionService sessionService,
    OpenAIPromptExecutionSettings executionSettings) : ControllerBase
{
    // ─── Costanti di configurazione della memoria ────────────────────────────
    private const int SogliaSummarization = 20; // Messaggi totali prima del compattamento
    private const int MessaggiDaPreservare = 6;  // Ultimi N messaggi sempre mantenuti intatti
    // ────────────────────────────────────────────────────────────────────────

    [HttpPost("chiedi")]
    public async Task<IActionResult> ChiediAllAnalista(
        [FromQuery] Guid utenteId,
        [FromBody] string domanda)
    {
        // ── Validazione input ───────────────────────────────────────────────
        if (utenteId == Guid.Empty)
            return BadRequest(new { Errore = "utenteId non può essere Guid.Empty." });

        if (string.IsNullOrWhiteSpace(domanda))
            return BadRequest(new { Errore = "La domanda non può essere vuota." });

        if (domanda.Length > 2000)
            return BadRequest(new { Errore = "La domanda supera i 2000 caratteri consentiti." });
        // ───────────────────────────────────────────────────────────────────

        // Il lock per-utente è gestito internamente da IChatSessionService
        await sessionService.AcquireLockAsync(utenteId);
        try
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // ── Recupera o inizializza la sessione ──────────────────────────
            var history = await sessionService.GetOrCreateHistoryAsync(
                utenteId,
                () => BuildInitialHistory(utenteId));

            // ── Sliding-window summarization ────────────────────────────────
            if (history.Count > SogliaSummarization)
            {
                history = await CompattaHistoryAsync(chatService, history, utenteId);
                await sessionService.SaveHistoryAsync(utenteId, history);
            }

            // ── Esecuzione richiesta ─────────────────────────────────────────
            history.AddUserMessage(domanda);

            var risposta = await chatService.GetChatMessageContentAsync(
                history, executionSettings, kernel);

            history.AddAssistantMessage(risposta.Content ?? string.Empty);

            await sessionService.SaveHistoryAsync(utenteId, history);

            return Ok(new { Analista = risposta.Content });
        }
        catch (Exception ex)
        {
            // In produzione: loggare con ILogger e restituire un ProblemDetails
            return StatusCode(500, new { Errore = "Errore interno.", Dettaglio = ex.Message });
        }
        finally
        {
            sessionService.ReleaseLock(utenteId);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // HELPER: sliding-window + summarization
    // ────────────────────────────────────────────────────────────────────────

    private async Task<ChatHistory> CompattaHistoryAsync(
        IChatCompletionService chatService,
        ChatHistory history,
        Guid utenteId)
    {
        // Separa messaggi da riassumere dagli ultimi N da conservare intatti
        var messaggiNonSystem = history
            .Where(m => m.Role != AuthorRole.System)
            .ToList();

        var vecchi = messaggiNonSystem.SkipLast(MessaggiDaPreservare);
        var recenti = messaggiNonSystem.TakeLast(MessaggiDaPreservare).ToList();

        var riassunto = await GeneraRiassuntoAsync(chatService, vecchi);

        // Ricostruisce la history: system → riassunto → messaggi recenti
        var nuovaHistory = BuildInitialHistory(utenteId);
        nuovaHistory.AddAssistantMessage($"📋 **CONTESTO SESSIONE PRECEDENTE**\n{riassunto}");

        foreach (var msg in recenti)
            nuovaHistory.Add(msg);

        return nuovaHistory;
    }

    private static async Task<string> GeneraRiassuntoAsync(
        IChatCompletionService chatService,
        IEnumerable<ChatMessageContent> messaggi)
    {
        var prompt = new ChatHistory();
        prompt.AddSystemMessage(
            "Sei un assistente finanziario specializzato. " +
            "Produci un riassunto ESECUTIVO in italiano (max 200 parole) che includa: " +
            "titoli discussi, prezzi/PMC menzionati, decisioni di investimento prese, " +
            "e qualsiasi dato numerico rilevante. Preserva tutti i ticker e i valori.");

        var testo = string.Join("\n", messaggi.Select(m => $"[{m.Role}]: {m.Content}"));
        prompt.AddUserMessage($"Riassumi questa conversazione:\n\n{testo}");

        var risultato = await chatService.GetChatMessageContentAsync(prompt);
        return risultato.Content ?? "(riassunto non disponibile)";
    }

    // ────────────────────────────────────────────────────────────────────────
    // SYSTEM PROMPT
    // ────────────────────────────────────────────────────────────────────────

    private static ChatHistory BuildInitialHistory(Guid utenteId)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(GetSystemMessage(utenteId));
        return history;
    }

    private static string GetSystemMessage(Guid utenteId)
    {
        var dataOggi = DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("it-IT"));

        return $"""
        Sei l'Analista Macro-Finanziario Senior dedicato all'utente {utenteId}.
        DATA CORRENTE: {dataOggi}.
        LINGUA: Rispondi SEMPRE in italiano. I termini tecnici finanziari restano in inglese.

        ═══════════════════════════════════════════
        OBIETTIVO PRIMARIO
        ═══════════════════════════════════════════
        Fornire analisi decisionali proiettive e correlate alla geopolitica globale.
        Non limitarti a riportare dati: interpreta gli eventi (es. vendite dei manager, acquisizioni, rumors)
        per fornire un vantaggio competitivo all'utente.

        ═══════════════════════════════════════════
        FLUSSO OPERATIVO OBBLIGATORIO
        ═══════════════════════════════════════════
        STEP 1 — RICERCA WEB MULTIPLA (AGGRESSIVA):
                 Per ogni titolo esegui ricerche mirate alle news dell'ULTIMA ORA:
                   • "<TICKER> notizie {dataOggi}"
                   • "<TICKER> insider selling manager vendita azioni"
                   • "<TICKER> rumors sentiment mercato"
                 ⚠️ SE TROVI notizie di manager che vendono azioni o problemi legali, 
                 DEVI segnalarlo immediatamente nel Contesto Macro come segnale di allarme.
        STEP 2 — PORTAFOGLIO: Chiama 'GetComposizionePortafoglio' per i dati di carico.
        STEP 3 — PREZZI: Usa 'GetPrezzoAttuale' (Yahoo/Bloomberg).
        STEP 4 — CALCOLI: Determina P&L e distanza dai supporti tecnici.
        STEP 5 — RISPOSTA: Segui rigorosamente la struttura sotto.

        ═══════════════════════════════════════════
        REGOLE DI DATI E QUALITÀ
        ═══════════════════════════════════════════
        • Ticker Yahoo Finance: usa la sintassi corretta (es. BAMI.MI, FCT.MI).
        • GRAFICO: Scrivi "Visualizzo il grafico dell'andamento per [TICKER]" SOLO se richiesto o
          se necessario per mostrare un crollo/rialzo imminente.
        • INSIDER TRADING: La vendita di azioni da parte dei manager è un segnale ribassista critico. 
          Se rilevata, il rating deve riflettere questa diffidenza.
        • Strategia: Usa un tono da Report Bloomberg. Niente "potrebbe", usa "Si consiglia", "Azione:".

        ═══════════════════════════════════════════
        STRUTTURA RISPOSTA OBBLIGATORIA
        ═══════════════════════════════════════════
        1. 🔍 **Contesto Macro & News Alert**
           - Sintesi driver attuali.
           - 🚨 **ALERT**: Segnala qui eventuali vendite insider, rumors o news flash del giorno.

        2. 📊 **Analisi Portafoglio**
           - Confronto secco Prezzo attuale vs PMC utente.

        3. 📋 **Tabella Decisionale**
        | Ticker | Sentiment | Upside | Rating | Nota Tecnica |
        | :----- | :-------- | :----- | :----- | :----------- |

        4. 🛡️ **Risk Factors**
           - Identifica i 2 rischi specifici derivanti dalle ultime notizie trovate.

        5. 🎯 **Strategia Operativa & Action Plan**
           - **Catalizzatore**: (L'evento che muove il titolo oggi, es: "Uscita manager dal capitale").
           - **Azione**: [BUY / SELL / STRONG HOLD / ACCUMULATE] ben visibile.
           - **Livelli**: [ENTRY] x,xx EUR | [TARGET] x,xx EUR | [STOP LOSS] x,xx EUR.
           - **Analisi Senior**: Spiegazione brutale della strategia basata sulla notizia del giorno.

        ═══════════════════════════════════════════
        DOMANDE SUL MERCATO GIORNALIERO
        ═══════════════════════════════════════════
        (Struttura completa per "Borsa Oggi" come definita precedentemente...)

        INDICATORI VISIVI: 🚀 crescita | ⚡ alert insider | 🛡️ difensivo | 📉 ribasso
        STILE: Professionale, tagliente, orientato al profitto. Niente disclaimer legali.
        """;
    }
}