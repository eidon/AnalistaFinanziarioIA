using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TitoloController(ITitoloService _titoloService) : ControllerBase
{
    [HttpGet("cerca")]
    public async Task<IActionResult> Cerca([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<object>());

        // Chiamiamo il servizio che interroga Alpha Vantage
        var risultati = await _titoloService.CercaTitoliAsync(q);

        return Ok(risultati);
    }
}