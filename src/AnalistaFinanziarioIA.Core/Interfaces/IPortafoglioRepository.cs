using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IPortafoglioRepository
    {
        Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, string ticker);
        Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId);
        Task<decimal> CalcolaRendimentoTotaleAsync(int assetId);
    }
}
