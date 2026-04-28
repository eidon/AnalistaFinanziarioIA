using AnalistaFinanziarioIA.Core.DTOs;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Models;

namespace AnalistaFinanziarioIA.Core.Services;

public class TransazioneService(
    ITitoloRepository _titoloRepo,
    IPortafoglioRepository _portafoglioRepo,
    ITitoloService _titoloService) : ITransazioneService
{
    public async Task<bool> RegistraOperazioneAsync(TransazioneInputDto dto)
    {
        // 1. RISOLUZIONE TITOLO (Cerca o Crea)
        int idTitoloEffettivo = await RisolviIdTitoloAsync(dto.TitoloId, dto.TitoloLookup, dto.PrezzoUnitario);

        // 2. CREAZIONE ENTITÀ TRANSAZIONE
        var nuovaTransazione = MappaTransazione(dto);

        // 3. DELEGA AL REPOSITORY
        var assetAggiornato = await _portafoglioRepo.AggiungiTransazioneAsync(
            nuovaTransazione,
            dto.UtenteId,
            idTitoloEffettivo);

        return assetAggiornato != null;
    }

    // Metodo privato per centralizzare la logica di creazione/ricerca titolo
    private async Task<int> RisolviIdTitoloAsync(int? titoloId, TitoloLookupDto? lookup, decimal prezzoFallback)
    {
        if (titoloId.HasValue && titoloId.Value > 0)
            return titoloId.Value;

        if (lookup == null)
            throw new ArgumentException("Dati titolo insufficienti per la registrazione.");

        var titoloEsistente = await _titoloRepo.GetBySimboloAsync(lookup.Simbolo);
        if (titoloEsistente != null)
            return titoloEsistente.Id;

        // Creazione nuovo titolo
        var nuovoTitolo = new Titolo
        {
            Simbolo = lookup.Simbolo.ToUpper(),
            Nome = lookup.Nome,
            Isin = lookup.Isin,
            Valuta = lookup.Valuta ?? "EUR",
            DataCreazione = DateTime.UtcNow,
            DataUltimoPrezzo = DateTime.UtcNow,
            Mercato = lookup.Mercato,
            Settore = lookup.Settore ?? "Altro",
            UltimoPrezzo = lookup.PrezzoAttuale > 0 ? lookup.PrezzoAttuale : prezzoFallback,
            Tipo = lookup.Tipo != default
           ? lookup.Tipo
           : _titoloService.MappaTipoTitolo(null, lookup.Simbolo),
        };

        await _titoloRepo.AddAsync(nuovoTitolo);
        return nuovoTitolo.Id;
    }

    private Transazione MappaTransazione(TransazioneInputDto dto)
    {
        return new Transazione
        {
            TipoOperazione = Enum.IsDefined(typeof(TipoTransazione), dto.TipoOperazione)
                   ? dto.TipoOperazione
                   : TipoTransazione.Acquisto,
            Quantita = dto.Quantita,
            PrezzoUnitario = dto.PrezzoUnitario,
            Commissioni = dto.Commissioni,
            Tasse = dto.Tasse,
            Note = dto.Note ?? "Inserito via Service",
            Data = dto.Data ?? DateTime.UtcNow,
            TassoCambio = dto.TassoCambio > 0 ? dto.TassoCambio : 1.0m
        };
    }

    //public async Task<bool> RegistraDashboardAsync(RegistraTransazioneDto dto)
    //{
    //    // 1. RISOLUZIONE TITOLO (Cerca o Crea)
    //    int idTitoloEffettivo;

    //    if (dto.TitoloId.HasValue && dto.TitoloId.Value > 0)
    //    {
    //        idTitoloEffettivo = dto.TitoloId.Value;
    //    }
    //    else if (dto.TitoloLookup != null)
    //    {
    //        var titoloEsistente = await _titoloRepo.GetBySimboloAsync(dto.TitoloLookup.Simbolo);
    //        if (titoloEsistente != null)
    //        {
    //            idTitoloEffettivo = titoloEsistente.Id;
    //        }
    //        else
    //        {
    //            var nuovoTitolo = new Titolo
    //            {
    //                Simbolo = dto.TitoloLookup.Simbolo.ToUpper(),
    //                Nome = dto.TitoloLookup.Nome,
    //                Isin = dto.TitoloLookup.Isin,
    //                Valuta = dto.TitoloLookup.Valuta ?? "EUR",
    //                DataCreazione = DateTime.UtcNow,
    //                DataUltimoPrezzo = DateTime.UtcNow,
    //                Mercato = dto.TitoloLookup.Mercato,
    //                Settore = dto.TitoloLookup.Settore ?? "Altro",
    //                UltimoPrezzo = dto.TitoloLookup.PrezzoAttuale,
    //                Tipo = _titoloService.MappaTipoTitolo(dto.TitoloLookup.Tipo?.ToLower(), dto.TitoloLookup.Simbolo)
    //            };

    //            await _titoloRepo.AddAsync(nuovoTitolo);
    //            idTitoloEffettivo = nuovoTitolo.Id;
    //        }
    //    }
    //    else
    //    {
    //        throw new Exception("Dati titolo insufficienti.");
    //    }

    //    // 2. CREAZIONE ENTITÀ TRANSAZIONE
    //    var tipoOperazione = Enum.TryParse<TipoTransazione>(dto.TipoOperazione, ignoreCase: true, out var tipo)
    //        ? tipo
    //        : TipoTransazione.Acquisto;

    //    var nuovaTransazione = new Transazione
    //    {
    //        Tipo = tipoOperazione,
    //        Quantita = dto.Quantita,
    //        PrezzoUnitario = dto.PrezzoUnitario,
    //        Commissioni = dto.Commissioni,
    //        Tasse = dto.Tasse,
    //        Note = dto.Note ?? "Inserito da dashboard",
    //        Data = dto.Data,
    //        TassoCambio = 1.0m
    //    };

    //    // 3. DELEGA AL REPOSITORY
    //    var assetAggiornato = await _portafoglioRepo.AggiungiTransazioneAsync(
    //        nuovaTransazione,
    //        dto.UtenteId,
    //        idTitoloEffettivo);

    //    return assetAggiornato != null;
    //}
}
