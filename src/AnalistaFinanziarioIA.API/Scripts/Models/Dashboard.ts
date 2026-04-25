export interface IAssetDisplay {
    titoloId: number;
    ticker: string;
    titoloNome: string;
    quantita: number;
    pmc: number;                    // Prezzo Medio di Carico
    prezzoAttuale: number;          // Prezzo di mercato sincronizzato
    valoreDiMercato: number;        // quantita * prezzoAttuale
    guadagnoAssoluto: number;       // (prezzoAttuale - pmc) * quantita
    percentualeRendimento: number;
}

export interface IPerformanceStats {
    capitaleInvestito: number;
    costiTotali: number;
    guadagnoPrezzo: number;         // Profitto latente (asset ancora in possesso)
    guadagnoRealizzato: number;     // Profitto da vendite concluse
}

export interface IPortafoglioDashboard {
    utenteId: string;
    assets: IAssetDisplay[];
    totalePortafoglioEur: number;
    profittoTotaleEur: number;      // Somma di latente + realizzato
    percentualeTotale: number;
    statistiche: IPerformanceStats;
}