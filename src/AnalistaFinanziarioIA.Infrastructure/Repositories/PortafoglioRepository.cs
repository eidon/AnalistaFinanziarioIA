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

        if (transazione.Tipo == TipoTransazione.Vendita && asset == null)
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
        if (transazione.Tipo == TipoTransazione.Vendita && asset.QuantitaTotale < transazione.Quantita)
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
            if (t.Tipo == TipoTransazione.Acquisto)
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

            if (t.Tipo == TipoTransazione.Acquisto)
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

    public async Task<IEnumerable<Transazione>> GetStoriaFiltrataAsync(Guid utenteId, string? searchTerm)
    {
        var query = _context.Transazioni
            .Include(t => t.AssetPortafoglio)
                .ThenInclude(a => a!.Titolo)
            .Where(t => t.AssetPortafoglio.UtenteId == utenteId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var s = searchTerm.ToLower();
            query = query.Where(t =>
                (t.AssetPortafoglio.Titolo.Nome != null && t.AssetPortafoglio.Titolo.Nome.ToLower().Contains(s)) ||
                (t.AssetPortafoglio.Titolo.Simbolo != null && t.AssetPortafoglio.Titolo.Simbolo.ToLower().Contains(s)));
        }

        return await query
            .OrderByDescending(t => t.Data)
            .ToListAsync();
    }

    public async Task<List<AssetDisplayDto>> GetDashboardAssetsAsync(Guid utenteId)
    {
        var assets = await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Include(a => a.Transazioni)
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();

        var result = new List<AssetDisplayDto>();

        foreach (var asset in assets)
        {
            var acquisti = asset.Transazioni
                .Where(t => t.Tipo == TipoTransazione.Acquisto)
                .ToList();

            decimal pmcInEuro = 0;
            if (acquisti.Any())
            {
                decimal spesaTotaleEur = acquisti.Sum(t =>
                    t.Quantita * _valutaService.ConvertiInEur(t.PrezzoUnitario, asset.Titolo?.Valuta ?? "EUR")
                );
                decimal quantitaAcquistata = acquisti.Sum(t => t.Quantita);
                pmcInEuro = quantitaAcquistata > 0 ? spesaTotaleEur / quantitaAcquistata : 0;
            }

            decimal prezzoGrezzo = (asset.Titolo?.UltimoPrezzo > 0) ? asset.Titolo.UltimoPrezzo : pmcInEuro;
            decimal prezzoInEuro = _valutaService.ConvertiInEur(prezzoGrezzo, asset.Titolo?.Valuta ?? "EUR");

            result.Add(new AssetDisplayDto
            {
                AssetId = asset.Id,
                TitoloNome = asset.Titolo?.Nome ?? "N/A",
                Ticker = asset.Titolo?.Simbolo ?? "N/A",
                Quantita = asset.QuantitaTotale,
                Pmc = pmcInEuro,
                PrezzoAttuale = prezzoInEuro,
                Valuta = "EUR",
                ProfittoRealizzatoTotale = _valutaService.ConvertiInEur(asset.ProfittoRealizzatoTotale, asset.Titolo?.Valuta ?? "EUR")
            });
        }

        return result;
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
            Commissioni: transazioni.Sum(t => _valutaService.ConvertiInEur(t.Commissioni, t.AssetPortafoglio.Titolo.Valuta)),
            Tasse: transazioni.Sum(t => _valutaService.ConvertiInEur(t.Tasse, t.AssetPortafoglio.Titolo.Valuta))
        );
    }
}
