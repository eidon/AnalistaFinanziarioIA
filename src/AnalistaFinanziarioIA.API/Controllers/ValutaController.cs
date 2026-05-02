using AnalistaFinanziarioIA.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalistaFinanziarioIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValutaController(IValutaService _valutaService) : ControllerBase
    {

        [HttpGet("cambio")]
        public async Task<IActionResult> GetCambio([FromQuery] string da, [FromQuery] string a = "EUR")
        {
            if (string.IsNullOrEmpty(da)) return BadRequest("Valuta di origine mancante");

            var tasso = await _valutaService.GetTassoCambioAsync(da.ToUpper(), a.ToUpper());
            return Ok(new { tasso });
        }
    }
}