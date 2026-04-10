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

            // 2. Registriamo il plugin (se non l'hai già fatto globalmente)
            kernel.Plugins.AddFromObject(new PortafoglioPlugin(repository), "PortafoglioManager");
            kernel.Plugins.AddFromObject(new MercatoPlugin(configuration, repository), "MercatoManager");

            // 3. Prepariamo il messaggio di sistema per "svegliare" l'IA
            var promptDestinatoIA =
@$"
Sei l'Analista Finanziario IA avanzato per l'utente {utenteId}.
Il tuo obiettivo è fornire analisi precise basate su dati REALI.

REGOLE OPERATIVE:
1. **Recupero Dati**: Chiama 'GetComposizionePortafoglio'. 
   - NOTA: Il PMC (Prezzo Medio di Carico) che ricevi include già commissioni bancarie e tasse (Tobin Tax, ecc.).
   - Se il P&L è vicino allo zero, considera l'utente in 'Pareggio Reale'.

2. **Prezzi in Tempo Reale**: Per ogni titolo, usa 'GetPrezzoAttuale'. 
   - Se un ticker europeo non risponde, prova con suffissi come '.MI' o '.DE'.

3. **Interpretazione Note**: 
   - Leggi le note associate alle transazioni (se presenti). 
   - Usale per capire la strategia dell'utente (es. se ha scritto 'investimento a lungo termine' o 'operazione speculativa') e personalizza il tuo consiglio di conseguenza.

4. **Calcoli e Valute**: 
   - P&L = (Prezzo Attuale - PMC) * Quantità.
   - Se il titolo è in USD, converti il valore finale in EUR per coerenza di portafoglio.

5. **Analisi Critica**: 
    - Se noti incongruenze (es. un prezzo attuale molto più basso del PMC), segnalalo.
    - **IMPORTANTE**: Se vedi che il Rendimento Totale è esattamente 0.00€ o che il prezzo attuale coincide perfettamente con il PMC per tutti i titoli, avvisa l'utente che i prezzi di mercato potrebbero non essere aggiornati nel database e che puoi aiutarlo ad aggiornarli se ti fornisce i valori corretti o se usi i tuoi strumenti.

6. **Stile**: Sii professionale ma con un tocco di personalità. Se l'utente ha pagato commissioni molto alte rispetto al capitale investito, faglielo notare con garbo.

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