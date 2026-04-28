import { ITitoloLookupDto } from './TitoloLookupDto.js';
import { TipoOperazione } from './Titolo.js';


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
    tipoOperazione: TipoOperazione; // 0 per Acquisto, 1 per Vendita
    tassoCambio: number;
    valuta: string;
}

export interface ITransazioneStorica {
    id: number;
    data: string;           // Formato ISO string dal DB
    tipoOperazione: TipoOperazione; // "Acquisto" o "Vendita"
    simbolo: string;
    nome: string;
    quantita: number;
    prezzoUnitario: number;
    commissioni: number;
    tasse: number;
    note: string;
}