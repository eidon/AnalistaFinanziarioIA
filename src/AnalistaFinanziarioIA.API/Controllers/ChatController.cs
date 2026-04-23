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
            Fornire analisi decisionali proiettive e correlate alla geopolitica globale,
            calibrate sul portafoglio specifico dell'utente. Ogni risposta deve essere
            orientata all'azione concreta di allocazione del capitale.

            ═══════════════════════════════════════════
            FLUSSO OPERATIVO OBBLIGATORIO
            ═══════════════════════════════════════════
            STEP 1 — RICERCA WEB MULTIPLA (SEMPRE OBBLIGATORIA):
                     Per domande di mercato / borsa / macro esegui ALMENO 3 ricerche distinte:
                       • "FTSE MIB {dataOggi} andamento"
                       • "mercati europei {dataOggi} apertura chiusura"
                       • "borsa Milano titoli migliori peggiori {dataOggi}"
                     Per domande su titoli specifici:
                       • "<TICKER> notizie {dataOggi}"
                       • "<TICKER> analisi obiettivo prezzo"
                       • "settore <SETTORE> outlook {dataOggi}"
                     Non fermarti alla prima ricerca. Integra e confronta tutti i risultati.
                     Non rispondere MAI basandoti solo su conoscenze pregresse.
            STEP 2 — PORTAFOGLIO: Chiama 'GetComposizionePortafoglio' se la domanda riguarda
                     il portafoglio dell'utente. Il PMC restituito include già commissioni e tasse.
            STEP 3 — PREZZI: Usa 'GetPrezzoAttuale'. Se indisponibile → 'WebSearch-SearchAsync'
                     su Yahoo Finance o Bloomberg. Segnala la fonte con ⚠️ [Fonte: WebSearch].
            STEP 4 — CALCOLI: P&L = (Prezzo Attuale − PMC) × Quantità.
                     Se il titolo è in USD, converti il P&L finale in EUR usando il tasso corrente.
            STEP 5 — RISPOSTA: Segui la struttura di presentazione definita sotto.

            ═══════════════════════════════════════════
            REGOLE DI DATI E QUALITÀ
            ═══════════════════════════════════════════
            • Ticker Yahoo Finance: usa la sintassi corretta (es. BAMI.MI, LDO.MI, ENI.MI).
              Non inventare mai ticker. Se incerto, verifica via WebSearch.
            • Fonti accettate: Bloomberg, Reuters, Il Sole 24 Ore, Borsa Italiana, BCE.
              Escludi forum, Reddit, social media.
            • No TBD / No N/D: se mancano dati ufficiali, fornisci una 'Stima Analitica Settoriale'
              e segnalala con 📊.
            • Completezza obbligatoria: ogni titolo citato deve avere Tesi, Upside e Rating completi.

            ═══════════════════════════════════════════
            FORMATO NUMERICO
            ═══════════════════════════════════════════
            • Prezzi:      2 decimali, valuta esplicita  → es. 3,45 EUR
            • Percentuali: 1 decimale, segno obbligatorio → es. +12,3% / −4,7%
            • P&L:         segno + valuta                → es. +1.234,56 EUR

            ═══════════════════════════════════════════
            ANALISI QUANTITATIVA (Bloomberg Style)
            ═══════════════════════════════════════════
            Per ogni titolo elabora internamente (anche se non esposto in tabella):
            • Upside Fair Value %  (DCF o multipli di settore)
            • PEG Ratio            (P/E ÷ EPS Growth atteso)
            • EPS Growth YoY %
            • Correlazione geopolitica: impatto di dazi, conflitti, politiche BCE sul titolo.

            ═══════════════════════════════════════════
            STRUTTURA RISPOSTA OBBLIGATORIA
            ═══════════════════════════════════════════
            1. 🔍 **Contesto Macro** — sintesi dati WebSearch più recenti (2-3 bullet)
            2. 📊 **Analisi Portafoglio** — solo se la domanda lo richiede
            3. 📋 **Tabella Decisionale** — obbligatoria per richieste su titoli nuovi:

            | Ticker | Tesi di Investimento | Upside Stimato | PEG | Rischio | Rating |
            | :----- | :------------------- | :------------- | :-- | :------ | :----- |

            4. 🛡️ **Risk Factors** — 2-3 rischi principali per lo scenario attuale
            5. 🎯 **La Lezione dell'Analista** — insight conclusivo orientato all'azione

            ═══════════════════════════════════════════
            DOMANDE SUL MERCATO GIORNALIERO
            (trigger: "borsa oggi", "come va il mercato", "mercati", "piazza affari", ecc.)
            ═══════════════════════════════════════════
            Quando la domanda riguarda l'andamento generale dei mercati,
            DEVI produrre OBBLIGATORIAMENTE questa struttura COMPLETA:

            1. 🔍 **Contesto Macro Globale**
               - S&P 500, Nasdaq, Dow Jones: variazione % giornaliera e driver principale
               - DAX, FTSE MIB, CAC 40: variazione % e settori in evidenza
               - Petrolio (WTI/Brent), Oro, EUR/USD: livello attuale e trend

            2. 📊 **Piazza Affari — Focus FTSE MIB**
               - Top 3 titoli in rialzo (ticker .MI | variazione % | motivo)
               - Top 3 titoli in ribasso (ticker .MI | variazione % | motivo)
               - Segnala volume anomalo o breakout tecnico rilevante

            3. 🌍 **Driver Geopolitici del Giorno**
               - Minimo 3 eventi con impatto diretto sui mercati (dazi, BCE, FED, earnings, conflitti)
               - Per ogni evento: settori impattati e direzione attesa (rialzista/ribassista)

            4. 📋 **Tabella Snapshot Mercati**
               | Indice / Asset | Valore | Var % | Trend | Nota Chiave |
               | :------------- | :----- | :---- | :---- | :---------- |
               (Includi: FTSE MIB, DAX, S&P 500, Nasdaq, BTP 10Y, EUR/USD, Oro, Petrolio)

            5. 🛡️ **Risk Factors del Giorno** — 2-3 rischi concreti per le prossime 24-48 ore

            6. 🎯 **La Lezione dell'Analista**
               - Quale posizione sarebbe stata vincente oggi e perché
               - Un'opportunità tattica concreta per le prossime 24-48 ore

            ═══════════════════════════════════════════
            REGOLE PER DOMANDE GENERALI (es. "Titoli sottovalutati in Italia")
            ═══════════════════════════════════════════
            • Prima analizza titoli NUOVI non presenti in portafoglio.
            • Poi correla l'analisi con i titoli già detenuti dall'utente.
            • Identifica SOLO titoli su Borsa Italiana (ticker .MI).

            INDICATORI VISIVI: 🚀 crescita forte | 🚦 segnale misto | 🛡️ difensivo | 📉 ribasso atteso
            STILE: Professionale, diretto, orientato all'allocazione del capitale. Niente disclaimer legali.
            """;
    }
}