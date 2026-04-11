using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Interfaces
{
    public interface IPortafoglioRepository
    {
        Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId);
        Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId);
        Task<decimal> CalcolaRendimentoTotaleAsync(int assetId, decimal prezzoAttuale);
        Task<Titolo?> GetTitoloBySimboloAsync(string simbolo);
        Task UpdateTitoloAsync(Titolo titolo);
        Task<bool> EliminaTransazioneAsync(int transazioneId);
        Task<IEnumerable<Transazione>> GetStoriaFiltrataAsync(Guid utenteId, string? searchTerm);
    }
}
