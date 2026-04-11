using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class PortafoglioRepository(AnalistaFinanziarioDbContext _context) : IPortafoglioRepository
{

    public async Task<AssetPortafoglio> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId)
    {
        // 1. Verifica esistenza titolo
        var titolo = await _context.Titoli.FindAsync(titoloId)
            ?? throw new Exception("Titolo non trovato.");

        // 2. Cerchiamo l'asset 
        var asset = await _context.AssetsPortafoglio
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
            _context.AssetsPortafoglio.Add(asset);
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

        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetPortafoglioUtenteAsync(Guid utenteId)
    {
        return await _context.AssetsPortafoglio
            .Include(a => a.Titolo)        // Per nome, simbolo e valuta
            .Include(a => a.Dividendi)     // Per il calcolo del cash flow (se l'IA lo analizza)
            .Include(a => a.Transazioni)   // <--- FONDAMENTALE per leggere Note, Tasse e Commissioni
            .Where(a => a.UtenteId == utenteId)
            .ToListAsync();
    }

    public async Task<decimal> CalcolaRendimentoTotaleAsync(int assetId, decimal prezzoAttuale)
    {
        var asset = await _context.AssetsPortafoglio
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
        return await _context.Titoli
            .FirstOrDefaultAsync(t => t.Simbolo == simbolo);
    }

    public async Task<decimal> CalcolaPMCRealTimeAsync(int assetId)
    {
        // Recuperiamo solo le transazioni di ACQUISTO per quel determinato asset
        var acquisti = await _context.Transazioni
            .Where(t => t.AssetPortafoglioId == assetId && t.Tipo == TipoTransazione.Acquisto)
            .ToListAsync();

        if (!acquisti.Any()) return 0;

        decimal spesaTotale = acquisti.Sum(t => t.Quantita * t.PrezzoUnita);
        decimal quantitaTotale = acquisti.Sum(t => t.Quantita);

        if (quantitaTotale == 0) return 0;

        return spesaTotale / quantitaTotale;
    }

    public async Task UpdateTitoloAsync(Titolo titolo)
    {
        _context.Titoli.Update(titolo);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EliminaTransazioneAsync(int transazioneId)
    {
        // 1. Trova la transazione
        var t = await _context.Transazioni.FindAsync(transazioneId);
        if (t == null) return false;

        // 2. Trova l'asset usando direttamente l'AssetPortafoglioId della transazione
        var asset = await _context.AssetsPortafoglio.FindAsync(t.AssetPortafoglioId);

        if (asset != null)
        {
            // 3. Aggiorna la quantità totale dell'asset
            // Uso 'Tipo' e 'TipoTransazione.Acquisto' basandomi sulla tua classe
            if (t.Tipo == TipoTransazione.Acquisto)
                asset.QuantitaTotale -= t.Quantita;
            else
                asset.QuantitaTotale += t.Quantita;

            // Se la quantità arriva a zero, potresti valutare se eliminare l'asset, 
            // ma di solito si lascia con quantità 0 per lo storico.
        }

        // 4. Rimuovi la transazione e salva
        _context.Transazioni.Remove(t);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AggiornaTransazioneAsync(int id, Transazione input)
    {
        var t = await _context.Transazioni.FindAsync(id);
        if (t == null) return false;

        var asset = await _context.AssetsPortafoglio.FindAsync(t.AssetPortafoglioId);

        if (asset != null)
        {
            // 1. Calcoliamo la differenza di quantità
            // Se la vecchia era 10 e la nuova è 15, dobbiamo aggiungere 5 all'asset.
            decimal differenzaQuantita = input.Quantita - t.Quantita;

            if (t.Tipo == TipoTransazione.Acquisto)
                asset.QuantitaTotale += differenzaQuantita;
            else
                asset.QuantitaTotale -= differenzaQuantita;
        }

        // 2. Aggiorniamo i dati della transazione
        t.Quantita = input.Quantita;
        t.PrezzoUnita = input.PrezzoUnita;
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
        // Usiamo Include e ThenInclude per caricare tutta la catena: Transazione -> Asset -> Titolo
        var query = _context.Transazioni
            .Include(t => t.AssetPortafoglio)
                .ThenInclude(a => a!.Titolo) // Il '!' dice al compilatore che ci aspettiamo che il titolo ci sia
            .Where(t => t.AssetPortafoglio.UtenteId == utenteId)
            .AsQueryable();

        // Applichiamo il filtro se l'utente ha scritto nella barra di ricerca
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
        // 1. Carichiamo gli asset con Titolo e Transazioni
        var assets = await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Include(a => a.Transazioni)
            .Where(a => a.UtenteId == utenteId && a.QuantitaTotale > 0)
            .ToListAsync();

        var result = new List<AssetDisplayDto>();

        foreach (var asset in assets)
        {
            // 2. Calcolo del PMC (Media Ponderata degli Acquisti)
            var acquisti = asset.Transazioni
                .Where(t => t.Tipo == TipoTransazione.Acquisto)
                .ToList();

            decimal pmc = 0;
            if (acquisti.Any())
            {
                decimal spesaTotale = acquisti.Sum(t => t.Quantita * t.PrezzoUnita);
                decimal quantitaAcquistata = acquisti.Sum(t => t.Quantita);
                pmc = quantitaAcquistata > 0 ? spesaTotale / quantitaAcquistata : 0;
            }

            // 3. Recupero del prezzo attuale direttamente dal Titolo (aggiornato da API/IA)
            decimal prezzoAttuale = (asset.Titolo?.UltimoPrezzo > 0)
                ? asset.Titolo.UltimoPrezzo
                : pmc; // Fallback sul PMC se il prezzo di mercato non è ancora disponibile

            // 4. Mappatura nel DTO
            // ValoreDiMercato e GuadagnoAssoluto si calcoleranno da soli grazie alle prop "=>"
            result.Add(new AssetDisplayDto
            {
                AssetId = asset.Id,
                TitoloNome = asset.Titolo?.Nome ?? "N/A",
                Ticker = asset.Titolo?.Simbolo ?? "N/A",
                Quantita = asset.QuantitaTotale,
                Pmc = pmc,
                PrezzoAttuale = prezzoAttuale,
                Valuta = asset.Titolo?.Valuta ?? "EUR"
            });
        }

        return result;
    }


}