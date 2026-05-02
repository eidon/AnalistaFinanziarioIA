using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class PortafoglioRepository(AnalistaFinanziarioDbContext _context, IValutaService _valutaService) : IPortafoglioRepository
{

    public async Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId)
    {
        // 1. Verifica esistenza titolo
        _ = await _context.Titoli.FindAsync(titoloId)
            ?? throw new Exception("Titolo non trovato.");

        // 2. Cerca o crea l'asset
        var asset = await _context.AssetsPortafoglio
            .FirstOrDefaultAsync(a => a.UtenteId == utenteId && a.TitoloId == titoloId);

        if (transazione.TipoOperazione == TipoTransazione.Vendita && asset == null)
            throw new Exception("Impossibile vendere un titolo che non è in portafoglio.");

        if (asset == null)
        {
            asset = new AssetPortafoglio
            {
                UtenteId = utenteId,
                TitoloId = titoloId,
                Transazioni = new List<Transazione>(),
                QuantitaTotale = 0,
                PrezzoMedioCarico = 0,
                ProfittoRealizzatoTotale = 0
            };
            _context.AssetsPortafoglio.Add(asset);
        }

        // 3. Validazione pre-vendita
        if (transazione.TipoOperazione == TipoTransazione.Vendita && asset.QuantitaTotale < transazione.Quantita)
            throw new Exception("Quantità insufficiente per la vendita.");

        // 4. Logica finanziaria delegata al Modello di Dominio (DDD)
        asset.ApplicaTransazione(transazione);

        // 5. Collegamento transazione all'asset
        asset.Transazioni ??= new List<Transazione>();
        asset.Transazioni.Add(transazione);

        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId)
    {
        return await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Include(a => a.Dividendi)
            .Include(a => a.Transazioni)
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();
    }

    public async Task<bool> EliminaTransazioneAsync(int transazioneId)
    {
        var t = await _context.Transazioni.FindAsync(transazioneId);
        if (t == null) return false;

        var asset = await _context.AssetsPortafoglio.FindAsync(t.AssetPortafoglioId);

        if (asset != null)
        {
            if (t.TipoOperazione == TipoTransazione.Acquisto)
                asset.QuantitaTotale -= t.Quantita;
            else
                asset.QuantitaTotale += t.Quantita;
        }

        _context.Transazioni.Remove(t);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Transazione?> GetTransazioneByIdAsync(int id)
    {
        return await _context.Transazioni
            .Include(t => t.AssetPortafoglio)
                .ThenInclude(a => a!.Titolo)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> AggiornaTransazioneAsync(int id, Transazione input)
    {
        var t = await _context.Transazioni.FindAsync(id);
        if (t == null) return false;

        var asset = await _context.AssetsPortafoglio.FindAsync(t.AssetPortafoglioId);

        if (asset != null)
        {
            decimal differenzaQuantita = input.Quantita - t.Quantita;

            if (t.TipoOperazione == TipoTransazione.Acquisto)
                asset.QuantitaTotale += differenzaQuantita;
            else
                asset.QuantitaTotale -= differenzaQuantita;
        }

        t.Quantita = input.Quantita;
        t.PrezzoUnitario = input.PrezzoUnitario;
        t.Commissioni = input.Commissioni;
        t.Tasse = input.Tasse;
        t.Note = input.Note;
        t.Data = input.Data;
        t.TassoCambio = input.TassoCambio;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TransazioneStoricaDto>> GetStoriaFiltrataAsync(Guid utenteId, string? search)
    {
        var query = _context.Transazioni
            .Include(t => t.AssetPortafoglio)
                .ThenInclude(a => a!.Titolo)
            .Where(t => t.AssetPortafoglio.UtenteId == utenteId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(t =>
                (t.AssetPortafoglio.Titolo.Nome != null && t.AssetPortafoglio.Titolo.Nome.Contains(s, StringComparison.CurrentCultureIgnoreCase)) ||
                (t.AssetPortafoglio.Titolo.Simbolo != null && t.AssetPortafoglio.Titolo.Simbolo.Contains(s, StringComparison.CurrentCultureIgnoreCase)));
        }

        return await query
                .OrderByDescending(t => t.Data)
                .Select(t => new TransazioneStoricaDto
                {
                    Id = t.Id,
                    Data = t.Data,
                    TipoOperazione = t.TipoOperazione, // Il campo rinominato
                    Quantita = t.Quantita,
                    PrezzoUnitario = t.PrezzoUnitario,
                    Commissioni = t.Commissioni,
                    Tasse = t.Tasse,
                    Note = t.Note ?? "",
                    Simbolo = t.AssetPortafoglio.Titolo.Simbolo,
                    Nome = t.AssetPortafoglio.Titolo.Nome
                })
                .ToListAsync();
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetDashboardAssetsAsync(Guid utenteId)
    {
        var assets = await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Include(a => a.Transazioni)
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();

        return assets;
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetAssetsAttiviAsync(Guid utenteId)
    {
        return await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Where(a => a.UtenteId == utenteId && a.QuantitaTotale > 0)
            .ToListAsync();
    }

    public async Task<(decimal Commissioni, decimal Tasse)> GetTotaleCostiTransazioniAsync(Guid utenteId)
    {
        var transazioni = await _context.Transazioni
            .Include(t => t.AssetPortafoglio)
            .ThenInclude(a => a.Titolo)
            .Where(t => t.AssetPortafoglio.UtenteId == utenteId)
            .ToListAsync();

        return (
            Commissioni: (await Task.WhenAll(transazioni.Select(t => _valutaService.ConvertiInEurAsync(t.Commissioni, t.AssetPortafoglio.Titolo.Valuta)))).Sum(),
            Tasse: (await Task.WhenAll(transazioni.Select(t => _valutaService.ConvertiInEurAsync(t.Tasse, t.AssetPortafoglio.Titolo.Valuta)))).Sum()
        );
    }
}
