using Microsoft.SemanticKernel;
using System.ComponentModel;
using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.API.Plugins;

public class PortafoglioPlugin(IPortafoglioRepository repository)
{
    [KernelFunction]
    // Aggiorniamo la descrizione: l'IA deve sapere che qui trova anche le note strategiche
    [Description("Recupera la composizione del portafoglio, inclusi PMC (con tasse/commissioni) e note strategiche dell'utente.")]
    public async Task<string> GetComposizionePortafoglio(Guid utenteId)
    {
        var assets = await repository.GetPortafoglioUtenteAsync(utenteId);

        if (assets == null || !assets.Any())
            return "Il portafoglio è attualmente vuoto.";

        // Costruiamo un report più ricco
        var report = assets.Select(a => {
            // Recuperiamo l'ultima nota per dare contesto alle intenzioni dell'utente
            var ultimaNota = a.Transazioni?.OrderByDescending(t => t.Data).FirstOrDefault()?.Note;
            var notaTesto = string.IsNullOrWhiteSpace(ultimaNota) ? "Nessuna nota inserita" : ultimaNota;

            return $"- Titolo: {a.Titolo?.Simbolo} ({a.Titolo?.Nome}), " +
                   $"Quantità: {a.QuantitaTotale:N2}, " +
                   $"PMC: {a.PrezzoMedioCarico:N2} {a.Titolo?.Valuta} (Costi inclusi), " +
                   $"Nota Strategica: {notaTesto}";
        });

        return "Ecco i dettagli del portafoglio aggiornati:\n" + string.Join("\n", report);
    }
}