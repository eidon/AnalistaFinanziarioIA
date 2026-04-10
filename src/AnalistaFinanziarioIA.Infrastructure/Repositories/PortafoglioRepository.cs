using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class PortafoglioRepository(AnalistaFinanziarioDbContext context) : IPortafoglioRepository
{

    public async Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId)
    {
        // 1. Verifica esistenza titolo
        var titolo = await context.Titoli.FindAsync(titoloId)
            ?? throw new Exception("Titolo non trovato.");

        // 2. Cerchiamo l'asset 
        var asset = await context.AssetsPortafoglio
            .FirstOrDefaultAsync(a => a.UtenteId == utenteId && a.TitoloId == titoloId);

        if (asset == null)
        {
            // Se l'asset non esiste, lo creiamo
            asset = new AssetPortafoglio
            {
                UtenteId = utenteId,
                TitoloId = titoloId,
                Transazioni = new List<Transazione>(),
                QuantitaTotale = 0,
                PrezzoMedioCarico = 0
            };
            context.AssetsPortafoglio.Add(asset);
        }

        // 3. Logica Finanziaria (Corretta con Tasse e Commissioni)
        if (transazione.Tipo == TipoTransazione.Acquisto)
        {
            decimal costoEffettivoNuovo = (transazione.Quantita * transazione.PrezzoUnita)
                                          + transazione.Commissioni
                                          + transazione.Tasse;

            decimal costoTotalePrecedente = asset.QuantitaTotale * asset.PrezzoMedioCarico;

            asset.QuantitaTotale += transazione.Quantita;
            asset.PrezzoMedioCarico = (costoTotalePrecedente + costoEffettivoNuovo) / asset.QuantitaTotale;
        }
        else if (transazione.Tipo == TipoTransazione.Vendita)
        {
            if (asset.QuantitaTotale < transazione.Quantita)
                throw new Exception("Quantità insufficiente per la vendita.");

            asset.QuantitaTotale -= transazione.Quantita;
            // Il PMC non cambia durante una vendita
        }

        // 4. Il "trucco" per il salvataggio senza ID diretti:
        // Aggiungendo la transazione alla lista 'Transazioni' dell'asset, 
        // Entity Framework imposterà automaticamente la Foreign Key (AssetPortafoglioId) 
        // sulla transazione durante il SaveChangesAsync().
        if (asset.Transazioni == null) asset.Transazioni = new List<Transazione>();

        asset.Transazioni.Add(transazione);

        await context.SaveChangesAsync();
        return asset;
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId)
    {
        return await context.AssetsPortafoglio
            .Include(a => a.Titolo)        // Per nome, simbolo e valuta
            .Include(a => a.Dividendi)     // Per il calcolo del cash flow (se l'IA lo analizza)
            .Include(a => a.Transazioni)   // <--- FONDAMENTALE per leggere Note, Tasse e Commissioni
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();
    }

    public async Task<decimal> CalcolaRendimentoTotaleAsync(int assetId, decimal prezzoAttuale)
    {
        var asset = await context.AssetsPortafoglio
            .Include(a => a.Dividendi)
            .FirstOrDefaultAsync(a => a.Id == assetId)
            ?? throw new Exception("Asset non trovato");

        decimal valoreAttualeMercato = asset.QuantitaTotale * prezzoAttuale;

        // Calcoliamo i dividendi NETTI
        decimal dividendiNetti = asset.Dividendi.Sum(d => d.ImportoLordo - d.TasseTrattenute);

        decimal costoTotaleAcquisto = asset.QuantitaTotale * asset.PrezzoMedioCarico;

        // (Valore oggi + Cedole incassate) - (Quanto ho pagato tra acquisto, tasse e commissioni)
        return (valoreAttualeMercato + dividendiNetti) - costoTotaleAcquisto;
    }

    public async Task<Titolo?> GetTitoloBySimboloAsync(string simbolo)
    {
        return await context.Titoli
            .FirstOrDefaultAsync(t => t.Simbolo == simbolo);
    }

    public async Task UpdateTitoloAsync(Titolo titolo)
    {
        context.Titoli.Update(titolo);
        await context.SaveChangesAsync();
    }

}