using Microsoft.SemanticKernel;
using System.ComponentModel;
using AnalistaFinanziarioIA.Core.Interfaces;

namespace AnalistaFinanziarioIA.API.Plugins;

public class PortafoglioPlugin(IPortafoglioRepository repository)
{
    [KernelFunction]
    [Description("Recupera la composizione attuale del portafoglio di un utente per ticker, quantità e prezzo medio.")]
    public async Task<string> GetComposizionePortafoglio(Guid utenteId)
    {
        var assets = await repository.GetPortafoglioUtenteAsync(utenteId);

        if (!assets.Any()) return "Il portafoglio è vuoto.";

        var report = assets.Select(a =>
            $"- Titolo: {a.Titolo?.Nome}, Quantità: {a.QuantitaTotale:N2}, PMC: {a.PrezzoMedioCarico:N2} {a.Titolo?.Valuta}");

        return "Ecco i titoli in portafoglio:\n" + string.Join("\n", report);
    }
}