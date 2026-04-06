using AnalistaFinanziarioIA.API.Plugins;
using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(Kernel kernel, IPortafoglioRepository repository, IConfiguration configuration) : ControllerBase
{
    [HttpPost("chiedi")]
    public async Task<IActionResult> ChiediAllAnalista([FromQuery] Guid utenteId, [FromBody] string domanda)
    {
        try
        {
            // 1. Definiamo le impostazioni di esecuzione (Fondamentale!)
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
            var mercatoPlugin = new MercatoPlugin(configuration);

            // 2. Registriamo il plugin (se non l'hai già fatto globalmente)
            kernel.Plugins.AddFromObject(new PortafoglioPlugin(repository), "PortafoglioManager");
            kernel.Plugins.AddFromObject(mercatoPlugin, "MercatoManager");

            // 3. Prepariamo il messaggio di sistema per "svegliare" l'IA
            var promptDestinatoIA = 
@$"
Sei l'Analista Finanziario IA avanzato per l'utente {utenteId}.
Il tuo obiettivo è fornire analisi precise basate su dati REALI.

REGOLE OPERATIVE:
1. **Recupero Dati**: Prima di rispondere a qualsiasi domanda sul portafoglio, chiama SEMPRE 'GetComposizionePortafoglio'.
2. **Prezzi in Tempo Reale**: Per ogni titolo trovato, usa 'GetPrezzoAttuale'. 
    - Se un ticker europeo (es. VWCE) non restituisce dati, riprova aggiungendo '.DEX' o '.MI'.
3. **Gestione Valute**: 
    - Se un titolo è in USD (come NVDA), converti il suo valore in EUR usando il cambio attuale (o 1.08 se non riesci a recuperarlo) per poter sommare i totali.
    - Specifica sempre quando stai facendo una conversione.
4. **Calcoli**: 
    - Valore Posizione = Prezzo Attuale * Quantità.
    - P&L = (Prezzo Attuale - PMC) * Quantità.
    - Peso % = (Valore Posizione / Valore Totale Portafoglio) * 100.
5. **Analisi Critica**: Se noti incongruenze (es. un prezzo attuale molto più basso del PMC come nel caso di split azionari), segnalalo all'utente come possibile errore nei dati.

Domanda dell'utente: {domanda}";

            // 4. Invocazione corretta
            var risultato = await kernel.InvokePromptAsync(promptDestinatoIA, new KernelArguments(settings));

            return Ok(new { Analista = risultato.ToString() });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Errore = ex.Message });
        }
    }
}