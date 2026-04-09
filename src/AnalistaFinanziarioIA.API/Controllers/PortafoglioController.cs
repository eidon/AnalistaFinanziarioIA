using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortafoglioController(IPortafoglioRepository repository) : ControllerBase
{
    /// <summary>
    /// Registra un nuovo acquisto o vendita nel portafoglio.
    /// </summary>
    [HttpPost("transazione")]
    public async Task<IActionResult> RegistraOperazione(
        [FromQuery] Guid utenteId,
        [FromQuery] int titoloId, 
        [FromBody] Transazione transazione)
    {
        try
        {
            // 2. Passiamo l'int titoloId al repository
            var assetAggiornato = await repository.AggiungiTransazioneAsync(transazione, utenteId, titoloId);

            return Ok(new
            {
                Messaggio = "Operazione registrata con successo",
                // 3. Usiamo l'ID restituito dall'asset aggiornato o il parametro titoloId
                TitoloId = titoloId,
                NuovaQuantita = assetAggiornato.QuantitaTotale,
                NuovoPrezzoMedio = assetAggiornato.PrezzoMedioCarico
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Errore = ex.Message });
        }
    }



    /// <summary>
    /// Recupera la situazione attuale del portafoglio di un utente.
    /// </summary>
    [HttpGet("{utenteId}")]
    public async Task<IActionResult> GetPortafoglio(Guid utenteId)
    {
        var portafoglio = await repository.GetPortafoglioUtenteAsync(utenteId);
        return Ok(portafoglio);
    }
}