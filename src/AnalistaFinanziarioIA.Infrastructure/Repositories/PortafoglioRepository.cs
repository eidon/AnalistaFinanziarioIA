using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class PortafoglioRepository(AnalistaFinanziarioDbContext context) : IPortafoglioRepository
{
    public async Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, string ticker)
    {
        // 1. Troviamo il titolo nel database generale (es. Apple)
        var titolo = await context.Titoli.FirstOrDefaultAsync(t => t.Simbolo == ticker)
            ?? throw new Exception("Titolo non trovato a mercato. Aggiungilo prima ai Titoli.");

        // 2. Cerchiamo se l'utente ha già questo titolo in portafoglio
        var asset = await context.AssetsPortafoglio
            .Include(a => a.Transazioni)
            .FirstOrDefaultAsync(a => a.UtenteId == utenteId && a.TitoloId == titolo.Id);

        if (asset == null)
        {
            asset = new AssetPortafoglio { UtenteId = utenteId, TitoloId = titolo.Id };
            context.AssetsPortafoglio.Add(asset);
        }

        // 3. Logica di aggiornamento Quantità e Prezzo Medio
        if (transazione.Tipo == TipoTransazione.Acquisto)
        {
            decimal costoTotalePrecedente = asset.QuantitaTotale * asset.PrezzoMedioCarico;
            decimal costoNuovoAcquisto = transazione.Quantita * transazione.PrezzoUnita;

            asset.QuantitaTotale += transazione.Quantita;
            asset.PrezzoMedioCarico = (costoTotalePrecedente + costoNuovoAcquisto) / asset.QuantitaTotale;
        }
        else if (transazione.Tipo == TipoTransazione.Vendita)
        {
            asset.QuantitaTotale -= transazione.Quantita;
            // Il prezzo medio non cambia con la vendita, si realizza solo un gain/loss
        }

        // 4. Colleghiamo la transazione all'asset e salviamo
        asset.Transazioni.Add(transazione);
        await context.SaveChangesAsync();

        return asset;
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId)
    {
        return await context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Include(a => a.Dividendi)
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();
    }

    public Task<decimal> CalcolaRendimentoTotaleAsync(int assetId)
    {
        // Qui implementeremo la logica IA o matematica per il Total Return
        throw new NotImplementedException();
    }
}
