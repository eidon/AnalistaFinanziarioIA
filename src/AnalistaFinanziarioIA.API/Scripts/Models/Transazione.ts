export interface ITitoloLookupDto {
    simbolo: string;
    nome: string;
    isin?: string;
    prezzoAttuale: number;
    valuta: string;
    settore?: string;
    mercato?: string;
    tipo?: string;
}

export interface ITransazioneInputDto {
    utenteId: string;
    titoloId: number | null;
    titoloLookup: ITitoloLookupDto | null;
    quantita: number;
    prezzoUnitario: number;
    commissioni: number;
    tasse: number;
    note?: string;
    data: string; // Sarà il valore YYYY-MM-DD dell'input date
    tipoOperazione: number; // 0 per Acquisto, 1 per Vendita
}

export interface ITransazioneStorica {
    id: number;
    data: string;           // Formato ISO string dal DB
    tipo: number;           // 0 = Acquisto, 1 = Vendita
    tipoOperazione?: string; // "Acquisto" o "Vendita"
    ticker: string;
    titoloNome: string;
    quantita: number;
    prezzoUnitario: number;
    commissioni: number;
    tasse: number;
    note: string;
}