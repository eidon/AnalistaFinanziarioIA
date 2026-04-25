using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PortafoglioController(
    ITransazioneService _transazioneService,
    PortafoglioService _portafoglioService,
    IValidator<TransazioneInputDto> _validator) : ControllerBase
{
    /// <summary>
    /// Dashboard con totali e asset convertiti in EUR.
    /// </summary>
    [HttpGet("dashboard/{utenteId}")]
    public async Task<IActionResult> GetDashboard(Guid utenteId)
    {
        var dashboard = await _portafoglioService.GetDashboardCompletaAsync(utenteId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Registrazione di una nuova transazione (Acquisto/Vendita).
    /// </summary>
    [HttpPost("transazione")]
    public async Task<IActionResult> RegistraOperazione([FromBody] TransazioneInputDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        try
        {
            var successo = await _transazioneService.RegistraOperazioneAsync(dto);
            return Ok(new { Messaggio = "Operazione registrata con successo" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Errore = ex.Message });
        }
    }

    /// <summary>
    /// Storia delle transazioni raggruppata per Mese/Anno.
    /// </summary>
    [HttpGet("utente/{utenteId}")]
    public async Task<ActionResult<IEnumerable<StoriaTransazioniDto>>> GetStoria(Guid utenteId, [FromQuery] string? search)
    {
        // La logica di GroupBy e Mapping la spostiamo nel PortafoglioService
        var risultato = await _portafoglioService.GetStoriaRaggruppataAsync(utenteId, search);
        return Ok(risultato);
    }

    /// <summary>
    /// Lista degli asset attivi per l'utente (per dropdown o filtri).
    /// </summary>
    [HttpGet("my-assets/{utenteId}")]
    public async Task<IActionResult> GetMyAssets(Guid utenteId, [FromQuery] string? query)
    {
        // Anche questa logica di filtro e proiezione va nel Service
        var risultati = await _portafoglioService.GetAssetsFiltratiAsync(utenteId, query);
        return Ok(risultati);
    }
}