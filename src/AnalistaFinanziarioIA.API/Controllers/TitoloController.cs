using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitoloController(ITitoloService _titoloService) : ControllerBase
{

    [HttpGet("cerca")]
    public async Task<IActionResult> Cerca([FromQuery] string q, [FromQuery] string tipo = "nome")
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<TitoloLookupDto>());

        try
        {
            // ORA interroghiamo finalmente il servizio con FMP!
            if (tipo == "isin")
            {
                // Usiamo l'endpoint specifico per ISIN che abbiamo visto nella documentazione
                var risultati = await _titoloService.CercaPerIsinAsync(q);
                return Ok(risultati);
            }

            var baseRisultati = await _titoloService.CercaTitoliAsync(q);
            return Ok(baseRisultati);
        }
        catch (Exception ex)
        {
            // Logga l'errore per debug
            return StatusCode(500, $"Errore interno: {ex.Message}");
        }
    }


}