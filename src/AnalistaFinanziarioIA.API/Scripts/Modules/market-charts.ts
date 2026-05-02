import { QuotazioneStorica } from '../Models/Quotazione.js'; // Assicurati di avere o creare questo model

// Declare per Chart.js se lo carichi via CDN, altrimenti importa se usi npm
declare var Chart: any;

export class MarketChartModule {

    /**
     * Inizializza un grafico cercando ticker nel testo della chat
     */
    public static handleAiResponse(text: string, containerId: string): void {
        const keywords = ["grafico", "andamento", "visualizza", "storia", "prezzi", "chart"];
        const textLower = text.toLowerCase();
        const hasKeyword = keywords.some(k => textLower.includes(k));

        if (!hasKeyword) return;

        // Regex potenziata: cerca sequenze di maiuscole che possono avere un .MI o .DE finale
        // Supporta tickers come FCT.MI, ENI.MI, AAPL, ecc.
        const tickerRegex = /\b([A-Z]{1,5}(\.[A-Z]{2})?)\b/g;
        const matches = text.match(tickerRegex);

        console.log("[Chart Debug] Tickers trovati:", matches); // <-- AGGIUNGI QUESTO LOG

        if (matches && matches.length > 0) {
            // Pulizia: prendiamo il primo che non sia una parola comune (es. "EUR" o "USD")
            const ticker = matches.find(t => t !== 'EUR' && t !== 'USD' && t !== 'P&L');

            if (ticker) {
                console.log(`[Chart Debug] Tento di creare il grafico per: ${ticker}`);
                this.createChart(containerId, ticker);
            }
        }
    }

    private static async createChart(containerId: string, ticker: string): Promise<void> {
        const container = document.getElementById(containerId);
        if (!container) return;

        try {
            // Chiamata all'endpoint che abbiamo testato prima
            const response = await fetch(`/api/titoli/0/Quotazioni/storia-mercato/${ticker}?giorni=30`);
            if (!response.ok) return;

            const data: any[] = await response.json();
            if (data.length === 0) return;

            this.renderCanvas(container, ticker, data);
        } catch (error) {
            console.error(`[Chart Error] Errore caricamento dati per ${ticker}:`, error);
        }
    }

    private static renderCanvas(container: HTMLElement, ticker: string, data: any[]): void {
        // 1. Cerchiamo la "bolla" colorata (il div che contiene il testo)
        // Usiamo la classe 'rounded-2xl' che hai definito nella funzione appendMessage
        const bubble = container.querySelector('.rounded-2xl') as HTMLElement;

        // Creiamo il wrapper per il grafico
        const chartWrapper = document.createElement('div');
        chartWrapper.style.height = "220px";
        chartWrapper.style.width = "100%";
        chartWrapper.style.marginTop = "15px";
        chartWrapper.style.padding = "10px";

        // Cambiato in un colore scuro/trasparente per non accecare l'utente nel tema dark
        chartWrapper.style.backgroundColor = "rgba(0, 0, 0, 0.2)";
        chartWrapper.style.borderRadius = "8px";
        chartWrapper.style.border = "1px solid rgba(255, 255, 255, 0.1)";

        const canvas = document.createElement('canvas');
        chartWrapper.appendChild(canvas);

        // --- MODIFICA POSIZIONAMENTO ---
        if (bubble) {
            // Forza la bolla a mettere gli elementi in verticale (testo sopra, grafico sotto)
            bubble.style.display = "flex";
            bubble.style.flexDirection = "column";

            // Lo aggiungiamo DENTRO la bolla, in fondo
            bubble.appendChild(chartWrapper);
        } else {
            // Fallback: se non trova la bolla, lo mette dove può
            container.appendChild(chartWrapper);
        }
        // -------------------------------

        const labels = data.map(q => new Date(q.data).toLocaleDateString(undefined, { day: '2-digit', month: 'short' }));
        const prices = data.map(q => q.prezzoChiusura);

        const isUp = prices[prices.length - 1] >= prices[0];
        const color = isUp ? '#28a745' : '#dc3545';

        new Chart(canvas, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: ticker,
                    data: prices,
                    borderColor: color,
                    backgroundColor: isUp ? 'rgba(40, 167, 69, 0.1)' : 'rgba(220, 53, 69, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointRadius: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        ticks: { color: '#9ca3af', autoSkip: true, maxTicksLimit: 6 },
                        grid: { display: false }
                    },
                    y: {
                        position: 'right',
                        ticks: { color: '#9ca3af', font: { size: 10 } },
                        grid: { color: 'rgba(255, 255, 255, 0.05)' }
                    }
                }
            }
        });
    }

}