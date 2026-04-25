using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;
using AnalistaFinanziarioIA.Core.Services;
using AnalistaFinanziarioIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Repositories;

public class PortafoglioRepository(AnalistaFinanziarioDbContext _context, IValutaService _valutaService) : IPortafoglioRepository
{

    public async Task<AssetPortafoglio?> AggiungiTransazioneAsync(Transazione transazione, Guid utenteId, int titoloId)
    {
        // 1. Verifica esistenza titolo
        var titolo = await _context.Titoli.FindAsync(titoloId)
            ?? throw new Exception("Titolo non trovato.");

        // 2. Cerchiamo l'asset 
        var asset = await _context.AssetsPortafoglio
            .FirstOrDefaultAsync(a => a.UtenteId == utenteId && a.TitoloId == titoloId);

        // Se è una vendita e l'asset non esiste, errore immediato
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
                ProfittoRealizzatoTotale = 0 // Inizializziamo il contatore dei profitti "chiusi"
            };
            _context.AssetsPortafoglio.Add(asset);
        }

        // 3. Logica Finanziaria
        if (transazione.Tipo == TipoTransazione.Acquisto)
        {
            decimal costoEffettivoNuovo = (transazione.Quantita * transazione.PrezzoUnitario)
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

            // --- CALCOLO PROFITTO REALIZZATO (FISSAZIONE DEL PREZZO) ---
            // Ricavo netto vendita (Prezzo * Quantità - costi)
            decimal ricavoNettoVendita = (transazione.Quantita * transazione.PrezzoUnitario)
                                         - transazione.Commissioni
                                         - transazione.Tasse;

            // Costo storico della porzione venduta (PMC * Quantità)
            decimal costoStoricoVendita = asset.PrezzoMedioCarico * transazione.Quantita;

            // Il profitto realizzato che "esce" dal portafoglio e diventa cash
            decimal profittoDellaVendita = ricavoNettoVendita - costoStoricoVendita;

            // Accumuliamo nel totale realizzato dell'asset
            asset.ProfittoRealizzatoTotale += profittoDellaVendita;

            // Scaliamo la quantità
            asset.QuantitaTotale -= transazione.Quantita;
        }

        // 4. Gestione Transazione
        if (asset.Transazioni == null) asset.Transazioni = new List<Transazione>();
        asset.Transazioni.Add(transazione);

        // --- 5. LOGICA DI CHIUSURA POSIZIONE (Senza eliminazione) ---
        // Se la quantità è zero, l'asset non è più attivo.
        if (asset.QuantitaTotale <= 0)
        {
            asset.QuantitaTotale = 0; // Per sicurezza, se fosse negativa
            asset.PrezzoMedioCarico = 0; // Se non hai più azioni, il prezzo medio non ha più senso
        }

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

        decimal spesaTotale = acquisti.Sum(t => t.Quantita * t.PrezzoUnitario);
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
            .Where(a => a.UtenteId == utenteId) // && a.QuantitaTotale > 0)
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
                // CONVERTIAMO OGNI ACQUISTO IN EURO PRIMA DI FARE LA MEDIA
                decimal spesaTotaleEur = acquisti.Sum(t =>
                    (t.Quantita * _valutaService.ConvertiInEur(t.PrezzoUnitario, asset.Titolo?.Valuta ?? "EUR"))
                );
                decimal quantitaAcquistata = acquisti.Sum(t => t.Quantita);
                pmcInEuro = quantitaAcquistata > 0 ? spesaTotaleEur / quantitaAcquistata : 0;
            }

            // Prezzo attuale già convertito
            decimal prezzoGrezzo = (asset.Titolo?.UltimoPrezzo > 0) ? asset.Titolo.UltimoPrezzo : pmcInEuro;
            decimal prezzoInEuro = _valutaService.ConvertiInEur(prezzoGrezzo, asset.Titolo?.Valuta ?? "EUR");

            result.Add(new AssetDisplayDto
            {
                AssetId = asset.Id,
                TitoloNome = asset.Titolo?.Nome ?? "N/A",
                Ticker = asset.Titolo?.Simbolo ?? "N/A",
                Quantita = asset.QuantitaTotale,
                Pmc = pmcInEuro,              // PMC ORA È IN EURO
                PrezzoAttuale = prezzoInEuro, // PREZZO ATTUALE È IN EURO
                Valuta = "EUR",
                ProfittoRealizzatoTotale = _valutaService.ConvertiInEur(asset.ProfittoRealizzatoTotale, asset.Titolo?.Valuta ?? "EUR")
            });
        }

        return result;
    }

    public async Task<bool> RegistraOperazioneCompletaAsync(RegistraTransazioneDto dto)
    {
        // Usiamo una transazione SQL per garantire che se la creazione della 
        // transazione fallisce, non rimanga il titolo "orfano" o l'asset vuoto.
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            Titolo? titolo = null;

            // 1. GESTIONE TITOLO
            if (dto.TitoloId.HasValue && dto.TitoloId > 0)
            {
                titolo = await _context.Titoli.FindAsync(dto.TitoloId);
            }
            else if (dto.TitoloLookup != null)
            {
                // Verifichiamo se per caso il titolo esiste già per simbolo (es. LDO.MI)
                titolo = await GetTitoloBySimboloAsync(dto.TitoloLookup.Simbolo);

                if (titolo == null)
                {
                    // Creazione JIT (Just In Time) dell'anagrafica
                    titolo = new Titolo
                    {
                        Simbolo = dto.TitoloLookup.Simbolo.ToUpper(),
                        Nome = dto.TitoloLookup.Nome,
                        Isin = dto.TitoloLookup.Isin,
                        Settore = dto.TitoloLookup.Settore,
                        Mercato = dto.TitoloLookup.Mercato,
                        Valuta = dto.TitoloLookup.Valuta,
                        Tipo = Enum.Parse<TipoTitolo>(dto.TitoloLookup.Tipo),
                        UltimoPrezzo = dto.TitoloLookup.PrezzoAttuale, // <--- Salviamo il prezzo fresco di FMP
                        DataUltimoPrezzo = DateTime.Now,
                        DataCreazione = DateTime.Now
                    };
                    _context.Titoli.Add(titolo);
                    await _context.SaveChangesAsync();
                }
            }

            if (titolo == null) throw new Exception("Impossibile identificare o creare il titolo.");

            // 2. PREPARAZIONE TRANSAZIONE (Modello Core)
            var tipoInviato = Enum.TryParse<TipoTransazione>(dto.TipoOperazione, true, out var tipo)
                  ? tipo
                  : TipoTransazione.Acquisto;

            var nuovaTransazione = new Transazione
            {
                Tipo = tipoInviato,
                Quantita = dto.Quantita,
                PrezzoUnitario = dto.PrezzoUnitario,
                Commissioni = dto.Commissioni,
                Tasse = dto.Tasse,
                Data = dto.Data,
                Note = dto.Note ?? "Inserito da dashboard",
                TassoCambio = 1.0m // Iniziale, potresti espanderlo in futuro
            };

            // 3. LOGICA ASSET & PMC (Chiamata al tuo metodo esistente)
            // Questo metodo si occupa di:
            // - Cercare/Creare l'AssetPortafoglio (User <-> Titolo)
            // - Calcolare il nuovo PMC
            // - Scalare/Aumentare la QuantitàTotale
            await AggiungiTransazioneAsync(nuovaTransazione, dto.UtenteId, titolo.Id);

            await dbTransaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            // Logga l'errore se hai un logger, altrimenti lo rilanciamo per il controller
            throw new Exception($"Errore durante la registrazione: {ex.Message}");
        }
    }

    public async Task<IEnumerable<AssetPortafoglio>> GetAssetsAttiviAsync(Guid utenteId)
    {
        return await _context.AssetsPortafoglio
            .Include(a => a.Titolo)
            .Where(a => a.UtenteId == utenteId && a.QuantitaTotale > 0)
            .ToListAsync(); // Questo esegue la query e la trasforma in una lista
    }

    public async Task<(decimal Commissioni, decimal Tasse)> GetTotaleCostiTransazioniAsync(Guid utenteId)
    {
        var transazioni = await _context.Transazioni
            .Include(t => t.AssetPortafoglio)
            .ThenInclude(a => a.Titolo)
            .Where(t => t.AssetPortafoglio.UtenteId == utenteId)
            .ToListAsync();

        // Dobbiamo convertire ogni singola voce in base alla valuta del titolo associato
        return (
            Commissioni: transazioni.Sum(t => _valutaService.ConvertiInEur(t.Commissioni, t.AssetPortafoglio.Titolo.Valuta)),
            Tasse: transazioni.Sum(t => _valutaService.ConvertiInEur(t.Tasse, t.AssetPortafoglio.Titolo.Valuta))
        );
    }
}