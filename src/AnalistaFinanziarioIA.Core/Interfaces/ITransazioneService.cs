using AnalistaFinanziarioIA.Core.DTOs;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface ITransazioneService
    {
        /// <summary>Registra un'operazione di acquisto/vendita tramite il DTO del form portafoglio.</summary>
        Task<bool> RegistraOperazioneAsync(TransazioneInputDto dto);

        /// <summary>Registra un'operazione di acquisto/vendita tramite il DTO del form di registrazione rapida (dashboard).</summary>
        Task<bool> RegistraDashboardAsync(RegistraTransazioneDto dto);
    }
}
