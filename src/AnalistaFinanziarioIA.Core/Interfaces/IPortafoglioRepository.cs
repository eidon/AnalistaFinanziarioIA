using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IPortafoglioRepository
    {
        Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId);
        Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId);
        Task<decimal> CalcolaRendimentoTotaleAsync(int assetId);
    }
}
