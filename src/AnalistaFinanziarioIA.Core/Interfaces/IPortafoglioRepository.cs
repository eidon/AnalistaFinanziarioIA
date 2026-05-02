using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IPortafoglioRepository
    {
        Task<Transazione?> GetTransazioneByIdAsync(int id);

        Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId);
        Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId);
        Task<bool> EliminaTransazioneAsync(int transazioneId);
        Task<bool> AggiornaTransazioneAsync(int id, Transazione input);
        Task<IEnumerable<TransazioneStoricaDto>> GetStoriaFiltrataAsync(Guid utenteId, string? searchTerm);
        //Task<List<AssetDisplayDto>> GetDashboardAssetsAsync(Guid utenteId);
        Task<IEnumerable<AssetPortafoglio>> GetDashboardAssetsAsync(Guid utenteId);
        Task<IEnumerable<AssetPortafoglio>> GetAssetsAttiviAsync(Guid utenteId);
        Task<(decimal Commissioni, decimal Tasse)> GetTotaleCostiTransazioniAsync(Guid utenteId);
    }
}
