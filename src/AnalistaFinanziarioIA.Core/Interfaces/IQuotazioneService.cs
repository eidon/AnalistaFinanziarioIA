namespace AnalistaFinanziarioIA.Core.Interfaces;

public interface IQuotazioneService
{
    // Recupera l'ultimo prezzo e lo salva nel DB
    Task<decimal> AggiornaPrezzoTitoloAsync(int titoloId);
    
}
