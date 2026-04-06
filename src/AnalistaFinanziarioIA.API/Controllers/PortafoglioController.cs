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
        [FromQuery] string ticker,
        [FromBody] Transazione transazione)
    {
        try
        {
            // Il repository si occuperà di trovare il titolo, l'utente 
            // e ricalcolare il prezzo medio di carico (PMC)
            var assetAggiornato = await repository.AggiungiTransazioneAsync(transazione, utenteId, ticker);

            return Ok(new
            {
                Messaggio = "Operazione registrata con successo",
                Simbolo = ticker,
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