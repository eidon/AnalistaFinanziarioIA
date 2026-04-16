using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IPortafoglioRepository
    {
        Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId);
        Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId);
        Task<decimal> CalcolaRendimentoTotaleAsync(int assetId, decimal prezzoAttuale);
        Task<decimal> CalcolaPMCRealTimeAsync(int assetId);
        Task<Titolo?> GetTitoloBySimboloAsync(string simbolo);
        Task UpdateTitoloAsync(Titolo titolo);
        Task<bool> EliminaTransazioneAsync(int transazioneId);
        Task<bool> AggiornaTransazioneAsync(int id, Transazione input);
        Task<IEnumerable<Transazione>> GetStoriaFiltrataAsync(Guid utenteId, string? searchTerm);
        Task<List<AssetDisplayDto>> GetDashboardAssetsAsync(Guid utenteId);

        Task<bool> RegistraOperazioneCompletaAsync(RegistraTransazioneDto dto);
    }
}
