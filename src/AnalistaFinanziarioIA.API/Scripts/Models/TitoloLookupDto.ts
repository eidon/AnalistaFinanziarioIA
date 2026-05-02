import { TipoTitolo } from './Titolo.js'; 

// Corrisponde a: public class TitoloLookupDto : TitoloBaseDto
export interface ITitoloLookupDto {
    // Campi ereditati da TitoloBaseDto
    simbolo: string;
    nome: string;
    isin?: string;
    valuta: string;
    tipo: TipoTitolo;

    // Campi specifici di TitoloLookupDto
    id?: number;
    mercato: string;
    settore: string;
    prezzoAttuale: number;

    // Proprietà calcolata: public bool GiaPresenteNelDb => Id.HasValue;
    giaPresenteNelDb: boolean;
}