using AnalistaFinanziarioIA.Core.DTOs;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface ITransazioneService
    {
        Task<bool> RegistraOperazioneAsync(TransazioneInputDto dto);
    }
}
