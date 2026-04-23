export interface TitoloLookupDto {
    simbolo: string;
    nome: string;
    isin?: string;
    prezzoAttuale: number;
    valuta: string;
    settore?: string;
    mercato?: string;
    tipo?: string;
}

export interface TransazioneInputDto {
    utenteId: string;
    titoloId: number | null;
    titoloLookup: TitoloLookupDto | null;
    quantita: number;
    prezzoUnitario: number;
    commissioni: number;
    tasse: number;
    note?: string;
    data: string; // Sarà il valore YYYY-MM-DD dell'input date
    tipoOperazione: 'Acquisto' | 'Vendita'; // Invierai "Acquisto" o "Vendita"
}