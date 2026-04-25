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
        int idTitoloEffettivo;

        if (dto.TitoloId.HasValue && dto.TitoloId.Value > 0)
        {
            idTitoloEffettivo = dto.TitoloId.Value;
        }
        else if (dto.TitoloLookup != null)
        {
            var titoloEsistente = await _titoloRepo.GetBySimboloAsync(dto.TitoloLookup.Simbolo);
            if (titoloEsistente != null)
            {
                idTitoloEffettivo = titoloEsistente.Id;
            }
            else
            {
                var nuovoTitolo = new Titolo
                {
                    Simbolo = dto.TitoloLookup.Simbolo.ToUpper(),
                    Nome = dto.TitoloLookup.Nome,
                    Isin = dto.TitoloLookup.Isin,
                    Valuta = dto.TitoloLookup.Valuta ?? "EUR",
                    DataCreazione = DateTime.UtcNow,
                    DataUltimoPrezzo = DateTime.UtcNow,
                    Mercato = dto.TitoloLookup.Mercato,
                    Settore = dto.TitoloLookup.Settore ?? "Altro",
                    UltimoPrezzo = dto.PrezzoUnitario,
                    Tipo = _titoloService.MappaTipoTitolo(dto.TitoloLookup.Tipo?.ToLower(), dto.TitoloLookup.Simbolo)
                };

                await _titoloRepo.AddAsync(nuovoTitolo);
                idTitoloEffettivo = nuovoTitolo.Id;
            }
        }
        else
        {
            throw new Exception("Dati titolo insufficienti.");
        }

        // 2. CREAZIONE ENTITÀ TRANSAZIONE
        // Nota: Mappiamo l'operazione. Se non valida, default ad Acquisto come nel tuo vecchio controller
        var tipoOperazione = Enum.IsDefined(typeof(TipoTransazione), dto.TipoOperazione)
            ? (TipoTransazione)dto.TipoOperazione
            : TipoTransazione.Acquisto;

        var nuovaTransazione = new Transazione
        {
            Tipo = tipoOperazione,
            Quantita = dto.Quantita,
            PrezzoUnitario = dto.PrezzoUnitario,
            Commissioni = dto.Commissioni,
            Tasse = dto.Tasse,
            Note = dto.Note ?? "Inserito via Service",
            Data = dto.Data ?? DateTime.UtcNow,
            TassoCambio = 1.0m // Proprietà presente nel tuo Modello Transazione
        };

        // 3. DELEGA AL REPOSITORY (Logica Finanziaria & SaveChanges)
        // Usiamo il tuo metodo: AggiungiTransazioneAsync(Transazione, Guid, int)
        // Questo metodo nel tuo repository fa già tutto: calcolo PMC, profitto e SaveChanges()
        var assetAggiornato = await _portafoglioRepo.AggiungiTransazioneAsync(
            nuovaTransazione,
            dto.UtenteId,
            idTitoloEffettivo);

        return assetAggiornato != null;
    }
}