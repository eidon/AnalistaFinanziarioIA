/**
 * Rispecchia l'Enum C# AnalistaFinanziarioIA.Core.Models.TipoTitolo
 */
export enum TipoTitolo {
    Azione = 0,
    ETF = 1,
    BTP = 2,
    Crypto = 3,
    ObbligazioneCorp = 4
}

// Corrisponde a: public enum TipoOperazione (assunto dai DTO)
export enum TipoOperazione {
    Acquisto = 0,
    Vendita = 1
}

/**
 * Rispecchia la classe C# AnalistaFinanziarioIA.Core.Models.Titolo
 */
export interface ITitolo {
    id: number;
    simbolo: string;
    isin?: string;
    nome: string;
    settore?: string;
    mercato?: string;
    valuta: string;
    dataCreazione: string; // DateTime -> string (ISO)
    tipo: TipoTitolo;
    ultimoPrezzo: number;
    dataUltimoPrezzo: string;
}