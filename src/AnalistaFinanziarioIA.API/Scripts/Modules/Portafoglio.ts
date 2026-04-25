import { ITransazioneInputDto, ITitoloLookupDto, ITransazioneStorica } from '../Models/Transazione.js';
import { IPortafoglioDashboard, IAssetDisplay } from '../Models/Dashboard.js';

// Dichiariamo UTENTE_ID se è una variabile globale definita nell'HTML
declare const UTENTE_ID: string;

const valutaStyle = {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
};

let cacheMieiAssets: any[] = [];
let cacheStoriaTransazioni: ITransazioneStorica[] = [];

// Questa variabile memorizzerà l'ID della transazione che stiamo modificando
let idTransazioneInModifica: number | null = null;

export async function salvaTransazioneModale(): Promise<void> {
    const titoloIdEl = document.getElementById('titolo-selezionato-id') as HTMLInputElement | null;
    const titoloJsonEl = document.getElementById('titolo-selezionato-json') as HTMLInputElement | null;

    const titoloId = titoloIdEl?.value ? parseInt(titoloIdEl.value) : null;
    const titoloJsonRaw = titoloJsonEl?.value || "";
    const titoloLookup = titoloJsonRaw ? JSON.parse(titoloJsonRaw) : null;
    const dataEl = document.getElementById('m-data') as HTMLInputElement | null;

    const getFloat = (id: string) => {
        const el = document.getElementById(id) as HTMLInputElement | null;
        return el ? parseFloat(el.value || "0") : 0;
    };


    // Costruiamo il DTO
    const transazioneDto: ITransazioneInputDto = {
        utenteId: (window as any).UTENTE_ID,
        titoloId: titoloId,
        titoloLookup: titoloLookup,
        tipoOperazione: parseInt((document.getElementById('m-tipo-operazione') as HTMLInputElement).value),
        quantita: getFloat('m-quantita'),
        prezzoUnitario: getFloat('m-prezzo'),
        commissioni: getFloat('m-commissioni'),
        tasse: getFloat('m-tasse'),
        note: (document.getElementById('m-note') as HTMLInputElement | null)?.value || "",
        data: dataEl?.value || new Date().toISOString().split('T')[0]
    };

    if (!transazioneDto.quantita || !transazioneDto.prezzoUnitario) {
        alert("Inserire quantità e prezzo validi!");
        return;
    }

    try {
        // --- LOGICA DI SWITCH MODIFICA/INSERIMENTO ---
        let url = '/api/portafoglio/transazione';
        let metodo = 'POST';

        // 2. RIMOSSE le dichiarazioni 'let' da qui dentro!

        if (idTransazioneInModifica !== null) {
            url = `/api/transazioni/${idTransazioneInModifica}`;
            metodo = 'PUT';
        }

        const response = await fetch(url, {
            method: metodo,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(transazioneDto)
        });

        if (response.ok) {
            alert(idTransazioneInModifica ? "Modifica completata con successo!" : "Transazione registrata!");

            // 3. Reset dell'ID globale dopo il successo
            idTransazioneInModifica = null;

            if ((window as any).chiudiModale) (window as any).chiudiModale();
            await caricaPortafoglio();
        } else {
            if (typeof (window as any).gestisciErroreServer === 'function') {
                await (window as any).gestisciErroreServer(response);
            } else {
                const err = await response.text();
                alert("Errore dal server: " + err);
            }
        }
    } catch (error) {
        console.error("Errore fetch:", error);
    }
}

export async function eliminaTrasazione(id: number) {
    // Chiediamo conferma prima di procedere
    if (!confirm("Sei sicuro di voler eliminare questa transazione? Questa azione ricalcolerà il tuo portafoglio.")) return;

    try {
        const res = await fetch(`/api/transazioni/${id}`, {
            method: 'DELETE'
        });

        if (res.ok) {
            console.log(`Transazione ${id} eliminata correttamente.`);

            // Invece di fare location.reload(), chiamiamo caricaPortafoglio()
            // Questo aggiornerà sia la tabella Asset che la Storia Transazioni in un colpo solo
            await caricaPortafoglio();
        } else {
            const errorText = await res.text();
            alert("Errore durante l'eliminazione: " + errorText);
        }
    } catch (error) {
        console.error("Errore fetch eliminazione:", error);
    }
}

export function apriModaleNuova() {
    // 1. Reset dell'ID per evitare di sovrascrivere transazioni esistenti
    (window as any).idTransazioneInModifica = null;

    // 2. Ripristina i pulsanti BUY/SELL (Rimuove 'hidden')
    const containerTipo = document.getElementById('container-tipo-operazione');
    if (containerTipo) {
        containerTipo.classList.remove('hidden');
    }

    // 3. Reset del titolo e del testo del bottone
    const modalTitle = document.getElementById('modal-title');
    if (modalTitle) modalTitle.innerText = "Nuova Transazione";

    const btnSalva = document.getElementById('btn-salva-transazione');
    if (btnSalva) btnSalva.innerText = "Salva Transazione";

    // 4. Reset del Form (Casting a HTMLFormElement per correggere l'errore TS)
    const form = document.getElementById('form-transazione') as HTMLFormElement;
    if (form) {
        form.reset();
    }

    // 5. Assicurati che il tipo operazione torni su 'Acquisto' di default
    setTipoOperazione(0);

    // 6. Apri il modale (Casting ad any per correggere l'errore TS)
    (window as any).apriModale();
}


export async function preparaModificaTransazione(id: number) {
    try {
        const res = await fetch(`/api/transazioni/${id}`);
        if (!res.ok) throw new Error("Impossibile recuperare i dati");

        const t = await res.json();
        idTransazioneInModifica = t.id;

        // 3. Popoliamo i campi
        (document.getElementById('ricerca-titolo') as HTMLInputElement).value = t.assetPortafoglio?.titolo?.nome || "";
        (document.getElementById('m-quantita') as HTMLInputElement).value = t.quantita;
        (document.getElementById('m-prezzo') as HTMLInputElement).value = t.prezzoUnitario;
        (document.getElementById('m-commissioni') as HTMLInputElement).value = t.commissioni || 0;
        (document.getElementById('m-tasse') as HTMLInputElement).value = t.tasse || 0;

        const campoNote = document.getElementById('m-note') as HTMLTextAreaElement;
        if (campoNote) campoNote.value = t.note || "";

        const campoIsin = document.getElementById('m-isin') as HTMLInputElement;
        if (campoIsin) campoIsin.value = t.assetPortafoglio?.titolo?.isin || "";

        if (t.data) {
            (document.getElementById('m-data') as HTMLInputElement).value = t.data.split('T')[0];
        }

        // --- 4. GESTIONE UI (NASCONDI PULSANTI E CAMBIA TITOLO) ---
        const containerTipo = document.getElementById('container-tipo-operazione');
        if (containerTipo) {
            containerTipo.classList.add('hidden'); // Nasconde i pulsanti Buy/Sell
        }

        const modalTitle = document.getElementById('modal-title');
        if (modalTitle) {
            modalTitle.innerText = "Modifica Transazione"; // Pulisce il titolo
        }

        const btnSalva = document.getElementById('btn-salva-transazione');
        if (btnSalva) btnSalva.innerText = "Aggiorna Transazione";

        // 5. Apriamo il modale
        (window as any).apriModale();

    } catch (error) {
        console.error("Errore nel caricamento modifica:", error);
        alert("Errore nel recupero dei dati.");
    }
}

// --- 1. CARICAMENTO DASHBOARD ---
export async function caricaPortafoglio() {
    const UTENTE_ID = (window as any).UTENTE_ID || "C206BEC1-A5DE-4896-8472-3FF512635B3A";
    const url = `/api/portafoglio/dashboard/${UTENTE_ID}`;
    const storiaUrl = `/api/transazioni/utente/${UTENTE_ID}`;

    try {
        const [resDashboard, resStoria] = await Promise.all([
            fetch(url),
            fetch(storiaUrl)
        ]);

        if (!resDashboard.ok) throw new Error(`Errore API Dashboard: ${resDashboard.status}`);

        const data: IPortafoglioDashboard = await resDashboard.json();
        const storia = await resStoria.json();

        // 1. Aggiornamento Cache globale per filtri e ricerca vendita
        cacheStoriaTransazioni = storia;
        cacheMieiAssets = (data.assets || []).map(a => ({
            titoloId: a.titoloId,
            simbolo: a.ticker,
            nome: a.titoloNome,
            quantitaDisponibile: a.quantita
        }));

        // 2. Chiamata alle funzioni di aggiornamento UI
        aggiornaHeader(data);
        renderizzaTabellaAssets(data.assets || []);
        renderizzaTabellaStoria(storia);

        if (data.statistiche) {
            aggiornaSpecchiettoPerformance(data.statistiche);
        }

        // 3. Aggiornamento Grafico (se presente funzione globale)
        if ((window as any).aggiornaGrafico && data.assets?.length > 0) {
            const labels = data.assets.map(a => a.ticker);
            const values = data.assets.map(a => a.valoreDiMercato);
            (window as any).aggiornaGrafico(labels, values);
        }

    } catch (error) {
        console.error("Errore caricamento portafoglio:", error);
    }
}

/** Aggiorna i tre numeri grandi in alto: Valore, Profitto € e Profitto % */
function aggiornaHeader(data: IPortafoglioDashboard) {
    const elValore = document.getElementById('valore-totale-eur');
    const elProfitto = document.getElementById('profitto-totale-eur');
    const elPercentuale = document.getElementById('profitto-totale-perc');

    if (elValore) {
        elValore.innerText = `${data.totalePortafoglioEur.toLocaleString('it-IT', valutaStyle)}€`;
    }

    if (elProfitto) {
        const p = data.profittoTotaleEur;
        elProfitto.innerText = `${p >= 0 ? '+' : ''}${p.toLocaleString('it-IT', valutaStyle)}€`;
    }

    if (elPercentuale) {
        const perc = data.percentualeTotale;
        elPercentuale.innerText = `${perc >= 0 ? '+' : ''}${perc.toFixed(2)}%`;
        elPercentuale.className = perc >= 0 ? "text-sm text-green-500 font-medium" : "text-sm text-red-500 font-medium";
    }
}

/** Renderizza la tabella principale degli Asset posseduti */
function renderizzaTabellaAssets(assets: IAssetDisplay[]) {
    const tbody = document.getElementById('tabella-asset');
    if (!tbody) return;

    if (assets.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="p-4 text-center text-gray-500">Nessun asset in portafoglio</td></tr>';
        return;
    }

    tbody.innerHTML = assets.map(a => {
        const guadagno = a.guadagnoAssoluto || 0;
        return `
            <tr class="hover:bg-gray-750 border-b border-gray-800">
                <td class="px-4 py-3">
                    <div class="font-medium text-white">${a.ticker || 'N/A'}</div>
                    <div class="text-gray-500 text-xs">${a.titoloNome || ''}</div>
                </td>
                <td class="px-4 py-3 text-right">${a.quantita}</td>
                <td class="px-4 py-3 text-right text-gray-300">
                    € ${a.pmc.toLocaleString('it-IT', valutaStyle)}
                </td>
                <td class="px-4 py-3 text-right">
                    <div class="${guadagno >= 0 ? 'text-green-400' : 'text-red-400'}">
                        ${guadagno >= 0 ? '+' : ''}${guadagno.toLocaleString('it-IT', valutaStyle)}€
                    </div>
                </td>
                <td class="px-4 py-3 text-right text-gray-400 font-medium">
                    € ${a.prezzoAttuale.toLocaleString('it-IT', valutaStyle)}
                </td>
            </tr>`;
    }).join('');
}

/**
 * Si occupa solo di scrivere l'HTML nel tbody
 */
function renderizzaTabellaStoria(lista: ITransazioneStorica[]) {
    const tbodyStoria = document.getElementById('tabella-storia-body');
    if (!tbodyStoria) return;

    if (lista.length === 0) {
        tbodyStoria.innerHTML = '<tr><td colspan="5" class="p-4 text-center text-gray-500 italic">Nessun movimento trovato</td></tr>';
        return;
    }

    tbodyStoria.innerHTML = lista.map((t: ITransazioneStorica) => {
        const isVendita = t.tipo === 1;
        
        return `
        <tr class="hover:bg-gray-750 border-b border-gray-800 text-sm">
            <td class="px-6 py-4 text-gray-400">${new Date(t.data).toLocaleDateString()}</td>
            <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                    <span class="px-1.5 py-0.5 rounded text-[10px] font-bold ${isVendita ? 'bg-red-900/30 text-red-400' : 'bg-green-900/30 text-green-400'}">
                        ${isVendita ? 'SELL' : 'BUY'}
                    </span>
                    <div>
                        <div class="font-bold text-white">${t.ticker}</div>
                        <div class="text-xs text-gray-500">${t.titoloNome}</div>
                    </div>
                </div>
            </td>
            <td class="px-6 py-4 text-right">${t.quantita}</td>
            <td class="px-6 py-4 text-right">€${t.prezzoUnitario.toFixed(2)}</td>
            <td class="px-6 py-4 text-right">
                <div class="flex justify-end gap-3">
                    <button onclick="preparaModificaTransazione(${t.id})" class="text-blue-500 hover:text-blue-400">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                    </button>
                    <button onclick="eliminaTrasazione(${t.id})" class="text-red-500 hover:text-red-400">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-4v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}


/**
 * Filtra la storia in base a testo e tipo operazione
 */
export function applicaFiltriStoria() {
    const testo = (document.getElementById('filtro-ricerca') as HTMLInputElement)?.value.toLowerCase() || "";
    const filtroSelezionato = (document.getElementById('filtro-tipo') as HTMLSelectElement)?.value || "tutti";

    const filtrati = cacheStoriaTransazioni.filter(t => {
        // 1. Filtro testuale
        const matchTesto = t.ticker.toLowerCase().includes(testo) ||
            t.titoloNome.toLowerCase().includes(testo);

        // 2. Filtro per tipo (basato sul numero t.tipo)
        // Se filtroSelezionato è "tutti", passa sempre. 
        // Altrimenti confrontiamo il valore del select col numero t.tipo
        const matchTipo = filtroSelezionato === "tutti" || t.tipo.toString() === filtroSelezionato;

        return matchTesto && matchTipo;
    });

    console.log(`Filtrati ${filtrati.length} elementi su ${cacheStoriaTransazioni.length}`);
    renderizzaTabellaStoria(filtrati);
}

/**
 * Popola lo specchietto in basso a destra con i dati storici e i costi
 */
function aggiornaSpecchiettoPerformance(stats: any) {

    const format = (v: number) =>
        new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(v);

    // NOTA: Usiamo stats.capitaleInvestito (minuscolo come da console)
    const elCapitale = document.getElementById('perf-capitale');
    if (elCapitale) elCapitale.textContent = format(stats.capitaleInvestito || 0);

    const elCosti = document.getElementById('perf-costi');
    if (elCosti) elCosti.textContent = "-" + format(stats.costiTotali || 0);

    // Guadagno Prezzo (Latente)
    const elLatente = document.getElementById('perf-latente');
    if (elLatente) {
        const val = stats.guadagnoPrezzo || 0;
        elLatente.textContent = (val >= 0 ? "+" : "") + format(val);
        elLatente.className = `font-mono ${val >= 0 ? 'text-green-400' : 'text-red-400'}`;
    }

    // Guadagno Realizzato
    const elRealizzato = document.getElementById('perf-realizzato');
    if (elRealizzato) {
        const val = stats.guadagnoRealizzato || 0;
        elRealizzato.textContent = (val >= 0 ? "+" : "") + format(val);
        elRealizzato.className = `font-mono ${val >= 0 ? 'text-green-400' : 'text-red-400'}`;
    }

    // Rendimento Netto (Calcolato)
    const netto = (stats.guadagnoPrezzo || 0) + (stats.guadagnoRealizzato || 0) - (stats.costiTotali || 0);
    const elTotale = document.getElementById('perf-totale');
    if (elTotale) {
        elTotale.textContent = (netto >= 0 ? "+" : "") + format(netto);
        elTotale.className = `font-bold font-mono ${netto >= 0 ? 'text-green-400' : 'text-red-400'}`;
    }
}

// --- 2. RICERCA TITOLI (DEBOUNCED) ---
let timerDebounce: any;
export async function eseguiRicerca(testo: string) {
    const lista = document.getElementById('lista-suggerimenti');
    if (!lista) return;

    // Recuperiamo il tipo (0/1 o Acquisto/Vendita)
    const inputTipo = document.getElementById('m-tipo-operazione') as HTMLInputElement;
    const tipoOperazione = inputTipo ? inputTipo.value : 'Acquisto';

    // --- CASO VENDITA: Filtro locale immediato ---
    if (tipoOperazione === 'Vendita' || tipoOperazione === '1') {
        // Se il campo è vuoto, potresti anche decidere di mostrare TUTTI i tuoi asset
        // o nascondere la lista. Qui la nascondiamo se testo è vuoto:
        if (testo.length === 0) {
            lista.classList.add('hidden');
            return;
        }

        const query = testo.toLowerCase();
        const filtrati = cacheMieiAssets.filter(a =>
            (a.simbolo && a.simbolo.toLowerCase().includes(query)) ||
            (a.nome && a.nome.toLowerCase().includes(query))
        );

        renderizzaSuggerimenti(filtrati, true);
        return;
    }

    // --- CASO ACQUISTO: Ricerca API esterna (con limite 2 caratteri e debounce) ---
    clearTimeout(timerDebounce);

    if (testo.length < 2) {
        lista.classList.add('hidden');
        return;
    }

    timerDebounce = setTimeout(async () => {
        try {
            const response = await fetch(`/api/titolo/cerca?q=${encodeURIComponent(testo.trim())}`);
            const titoli = await response.json();
            renderizzaSuggerimenti(titoli, false);
        } catch (e) {
            console.error("Errore ricerca acquisto:", e);
        }
    }, 1000);
}

function renderizzaSuggerimenti(items: any[], isVendita: boolean) {
    const lista = document.getElementById('lista-suggerimenti');
    if (!lista) return;

    if (!items || items.length === 0) {
        lista.innerHTML = '<div class="p-4 text-sm text-gray-500 text-center italic">Nessun titolo trovato</div>';
        lista.classList.remove('hidden');
        return;
    }

    lista.innerHTML = items.map(t => {
        // --- UNIFORMAZIONE DATI (Cache vs API) ---
        const simbolo = t.simbolo || t.ticker || '';
        const nome = t.nome || t.titoloNome || '';
        const mercatoStr = t.mercato || '';
        const valuta = t.valuta || 'EUR';
        const qta = t.quantitaDisponibile || 0;

        // --- LOGICA BADGE E ICONE (La tua originale) ---
        const tipoGrezzo = (t.tipo || '').toLowerCase();
        const isEtf = ['trust', 'etf', 'fund', 'mutual_fund'].includes(tipoGrezzo);
        const icona = isEtf ? '📊' : '🏢';
        const tipoLabel = isEtf ? 'ETF' : 'AZIONE';
        const badgeClass = isEtf
            ? 'bg-purple-900/40 text-purple-400 border-purple-700/50'
            : 'bg-blue-900/40 text-blue-400 border-blue-700/50';

        // --- LOGICA MERCATO E BANDIERE (La tua originale) ---
        const isIta = mercatoStr.includes("MIL") || mercatoStr.includes("MI") || simbolo.endsWith(".MI");
        const bandiera = isIta ? '🇮🇹' : '🌍';
        const labelMercato = isIta ? 'MIL' : (mercatoStr || 'N/A');

        // Codifica dati per la selezione
        const dataB64 = btoa(unescape(encodeURIComponent(JSON.stringify(t))));

        return `
        <div onclick="selezionaTitolo('${dataB64}', ${qta})"
             class="flex items-center gap-3 p-3 hover:bg-gray-700/50 cursor-pointer border-b border-gray-800 last:border-0 transition group">
            
            <div class="bg-gray-800 w-10 h-10 rounded-lg flex items-center justify-center text-xl group-hover:scale-110 transition-transform">
                ${icona}
            </div>

            <div class="flex-1 overflow-hidden">
                <div class="flex items-center gap-2">
                    <span class="text-sm font-bold text-white uppercase">${simbolo}</span>
                    <span class="text-[9px] px-1.5 py-0.5 rounded border ${badgeClass} font-black uppercase tracking-wider">
                        ${tipoLabel}
                    </span>
                    ${isVendita ? `<span class="ml-auto text-[10px] text-green-400 font-bold bg-green-900/20 px-1.5 rounded">Possiedi: ${qta}</span>` : ''}
                </div>
                <div class="text-[11px] text-gray-400 truncate">${nome}</div>
            </div>

            <div class="text-right flex flex-col items-end gap-1">
                <div class="flex items-center gap-1 bg-gray-900/50 px-2 py-0.5 rounded-md border border-gray-700">
                    <span class="text-[12px]">${bandiera}</span>
                    <span class="text-[10px] font-bold text-gray-200">${labelMercato}</span>
                </div>
                <div class="text-[10px] text-gray-500 font-mono tracking-tighter">${valuta}</div>
            </div>
        </div>`;
    }).join('');

    lista.classList.remove('hidden');
}

// --- 3. SELEZIONE E ARRICCHIMENTO ---
// --- 3. SELEZIONE E ARRICCHIMENTO (Aggiornata per 0/1) ---
export async function selezionaTitolo(dataB64: string, quantitaPortafoglio: number = 0) {
    const titoloSelezionato = JSON.parse(atob(dataB64));
    console.log("Titolo selezionato:", titoloSelezionato);

    // Recuperiamo il tipo operazione dal tuo input hidden (0 o 1)
    const inputTipoOp = document.getElementById('m-tipo-operazione') as HTMLInputElement;
    const isVendita = inputTipoOp?.value === "1";

    // 1. Popoliamo l'ID del titolo
    const inputId = document.getElementById('titolo-selezionato-id') as HTMLInputElement;
    // Gli asset in cache usano 'titoloId', i titoli della ricerca globale usano 'id'
    const idDaUsare = titoloSelezionato.titoloId || titoloSelezionato.id || null;

    if (inputId) {
        inputId.value = idDaUsare ? idDaUsare.toString() : "";
    }

    // 2. UI: Nome del titolo nell'input di ricerca
    const inputRicerca = document.getElementById('ricerca-titolo') as HTMLInputElement;
    if (inputRicerca) {
        // Fallback tra 'titoloNome' (dalla cache assets) e 'nome' (dal DB globale)
        inputRicerca.value = titoloSelezionato.titoloNome || titoloSelezionato.nome || titoloSelezionato.Name || "";
    }

    document.getElementById('lista-suggerimenti')?.classList.add('hidden');

    // 3. Salviamo il JSON completo
    const inputJson = document.getElementById('titolo-selezionato-json') as HTMLInputElement;
    if (inputJson) {
        inputJson.value = JSON.stringify(titoloSelezionato);
    }

    // 4: POPOLAMENTO AUTOMATICO QUANTITÀ (Solo se Vendita)
    const inputQuantita = document.getElementById('m-quantita') as HTMLInputElement;

    if (inputQuantita && isVendita && quantitaPortafoglio > 0) {
        inputQuantita.value = quantitaPortafoglio.toString();

        // Feedback visivo: rosso per la vendita
        inputQuantita.classList.add('bg-red-900/20', 'ring-1', 'ring-red-500/50', 'transition-all');
        setTimeout(() => inputQuantita.classList.remove('bg-red-900/20', 'ring-1', 'ring-red-500/50'), 1500);
    }

    // 5. Arricchimento dati (Solo se è un nuovo acquisto senza ID)
    if (!idDaUsare && !isVendita) {
        try {
            const simbolo = titoloSelezionato.simbolo || titoloSelezionato.symbol || titoloSelezionato.ticker;
            if (simbolo) {
                const res = await fetch(`/api/titoli/dettaglio-completo/${simbolo}`);
                if (res.ok) {
                    const completo = await res.json();
                    console.log("Dati completi ricevuti:", completo);

                    // AGGIORNAMENTO JSON (per l'invio al server)
                    if (inputJson) inputJson.value = JSON.stringify(completo);

                    // --- AGGIORNAMENTO UI (per l'utente) ---
                    // Qui popoliamo l'ISIN visibile nella modale
                    const campoIsinUI = document.getElementById('m-isin') as HTMLInputElement;
                    if (campoIsinUI) {
                        campoIsinUI.value = completo.isin || "";

                        // Un tocco di classe: feedback visivo che il campo è stato popolato
                        campoIsinUI.classList.add('text-green-400');
                    }

                    // Se vuoi popolare anche il prezzo unitario suggerito
                    const inputPrezzo = document.getElementById('m-prezzo') as HTMLInputElement;
                    if (inputPrezzo && completo.prezzoAttuale > 0) {
                        inputPrezzo.value = completo.prezzoAttuale.toString();
                    }

                    // --- AGGIORNAMENTO VALUTA ---
                    const valuta = completo.valuta || "EUR";
                    const badgeValuta = document.getElementById('badge-valuta');
                    if (badgeValuta) {
                        badgeValuta.innerText = valuta;
                        // Se è diversa da EUR, potresti colorarla per attirare l'attenzione
                        badgeValuta.className = valuta !== 'EUR'
                            ? "absolute right-3 top-1/2 -translate-y-1/2 text-xs font-bold text-orange-400"
                            : "absolute right-3 top-1/2 -translate-y-1/2 text-xs font-bold text-gray-500";
                    }

                }
            }
        } catch (e) { console.error("Errore arricchimento:", e); }
    }
}

// --- 4. CHAT AI ---
export async function chiediAI() {
    const input = document.getElementById('chat-input') as HTMLInputElement;
    const domanda = input.value.trim();
    if (!domanda) return;

    appendMessage(domanda, 'user');
    input.value = '';
    showLoader();

    try {
        const UTENTE_ID = (window as any).UTENTE_ID || "C206BEC1-A5DE-4896-8472-3FF512635B3A";

        const res = await fetch(`/api/chat/chiedi?utenteId=${UTENTE_ID}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            // IMPORTANTE: ASP.NET Core vuole la stringa nuda JSON-friendly
            body: JSON.stringify(domanda)
        });

        if (!res.ok) {
            // Se fallisce ancora, stampiamo l'errore esatto del server per capire
            const errorDetail = await res.text();
            console.error("Dettaglio errore server:", errorDetail);
            throw new Error("Errore risposta AI");
        }

        const data = await res.json();
        removeLoader();

        const risposta = data.analista || data.risposta || (typeof data === 'string' ? data : "Risposta non valida");
        appendMessage(risposta, 'ai');

    } catch (e) {
        removeLoader();
        appendMessage("Spiacente, ho avuto un problema nel contattare il server.", 'ai');
        console.error(e);
    }
}

// --- FUNZIONI DI SUPPORTO CHAT ---
export function appendMessage(testo: string, mittente: 'user' | 'ai') {
    const chatWindow = document.getElementById('chat-window');
    if (!chatWindow) return;

    const div = document.createElement('div');
    div.className = `flex ${mittente === 'user' ? 'justify-end' : 'justify-start'} mb-4`;

    // Formattazione semplice per il testo (gestisce i ritorni a capo)
    const testoFormattato = testo.replace(/\n/g, '<br>');

    div.innerHTML = `
        <div class="max-w-[80%] p-3 rounded-2xl ${mittente === 'user'
            ? 'bg-blue-600 text-white rounded-tr-none'
            : 'bg-gray-700 text-gray-100 rounded-tl-none border border-gray-600'
        } shadow-md text-sm">
            ${testoFormattato}
        </div>
    `;
    chatWindow.appendChild(div);
    chatWindow.scrollTop = chatWindow.scrollHeight;
}

function showLoader() {
    const chatWindow = document.getElementById('chat-window');
    if (!chatWindow) return;
    const loader = document.createElement('div');
    loader.id = 'ai-loader';
    loader.className = 'flex justify-start mb-4 animate-pulse';
    loader.innerHTML = `
        <div class="bg-gray-700 p-3 rounded-2xl rounded-tl-none border border-gray-600 text-xs text-gray-400">
            L'analista sta elaborando i dati...
        </div>
    `;
    chatWindow.appendChild(loader);
    chatWindow.scrollTop = chatWindow.scrollHeight;
}

function removeLoader() {
    document.getElementById('ai-loader')?.remove();
}

export function mostraBenvenutoStatico() {
    
    const chatWindow = document.getElementById('chat-window');
    if (!chatWindow || chatWindow.children.length > 0) return;

    // Piccola animazione di caricamento per dare un tocco professionale
    const loader = document.createElement('div');
    loader.id = 'temp-loader';
    loader.className = 'flex justify-start mb-4 animate-pulse';
    loader.innerHTML = `
        <div class="bg-gray-700 p-3 rounded-2xl rounded-tl-none border border-gray-600 text-xs text-gray-400">
            Analista in linea...
        </div>
    `;
    chatWindow.appendChild(loader);

    setTimeout(() => {
        loader.remove();
        const messaggio = `Benvenuto nella tua Dashboard Finanziaria. 
        Sono il tuo Analista IA e ho accesso ai tuoi dati di portafoglio in tempo reale.

        Posso aiutarti a:
        • Analizzare le performance di Leonardo e Stellantis.
        • Calcolare l'impatto delle commissioni sui tuoi rendimenti.
        • Valutare la diversificazione del tuo capitale.

        Come posso esserti utile oggi?`;

        // Usiamo la funzione appendMessage che abbiamo già creato
        (window as any).appendMessage(messaggio, 'ai');
    }, 800);
}

async function gestisciErroreServer(response: Response) {
    try {
        const errorData = await response.json();
        console.log("Dati errore ricevuti:", errorData);

        let messaggi: string[] = [];

        // Questo gestisce il formato ToDictionary() -> { "Campo": ["Messaggio"] }
        // errorData in questo caso è un oggetto dove ogni proprietà è un array di stringhe
        if (errorData && typeof errorData === 'object' && !Array.isArray(errorData)) {
            for (const chiave in errorData) {
                const erroriCampo = errorData[chiave];
                if (Array.isArray(erroriCampo)) {
                    messaggi.push(...erroriCampo);
                } else if (typeof erroriCampo === 'string') {
                    messaggi.push(erroriCampo);
                }
            }
        }
        // Caso di fallback per array semplici
        else if (Array.isArray(errorData)) {
            messaggi = errorData.map(err => typeof err === 'string' ? err : (err.errorMessage || "Errore"));
        }

        const testoFinale = messaggi.length > 0 ? messaggi.join("\n") : "Errore di validazione generico.";
        alert("Attenzione:\n" + testoFinale);

    } catch (e) {
        console.error("Errore parsing JSON errori:", e);
        alert("Errore del server: " + response.statusText);
    }
}



async function setTipoOperazione(tipo: number) {
    // 1. Casting esplicito: diciamo a TS che sono Input e usiamo l'operatore '?' per il null check
    const inputTipo = document.getElementById('m-tipo-operazione') as HTMLInputElement | null;
    const inputRicerca = document.getElementById('ricerca-titolo') as HTMLInputElement | null;
    const inputIsin = document.getElementById('m-isin') as HTMLInputElement | null;
    const inputQuantita = document.getElementById('m-quantita') as HTMLInputElement | null;
    const inputPrezzo = document.getElementById('m-prezzo') as HTMLInputElement | null;

    // Elementi che non sono input (non hanno .value ma .innerText o .classList)
    const btnBuy = document.getElementById('btn-buy');
    const btnSell = document.getElementById('btn-sell');
    const btnSalva = document.getElementById('btn-salva-transazione');
    const badgeValuta = document.getElementById('badge-valuta');

    // 2. Uso sicuro delle proprietà
    if (inputTipo) inputTipo.value = tipo.toString();

    // Reset campi con protezione null-check
    if (inputRicerca) inputRicerca.value = "";
    if (inputIsin) {
        inputIsin.value = "";
        inputIsin.classList.remove('text-green-400');
    }
    if (inputQuantita) inputQuantita.value = "";
    if (inputPrezzo) inputPrezzo.value = "";

    if (badgeValuta) badgeValuta.innerText = "";


    if (tipo === 0) { // ACQUISTO
        btnBuy?.classList.add('bg-blue-600', 'text-white');
        btnBuy?.classList.remove('text-gray-400');

        btnSell?.classList.remove('bg-red-600', 'text-white');
        btnSell?.classList.add('text-gray-400');

        // Usiamo add/remove invece di replace per sicurezza al primo caricamento
        btnSalva?.classList.remove('bg-red-600');
        btnSalva?.classList.add('bg-blue-600', 'text-white');
        if (btnSalva) btnSalva.textContent = "Salva Acquisto";

    } else { // VENDITA
        btnSell?.classList.add('bg-red-600', 'text-white');
        btnSell?.classList.remove('text-gray-400');

        btnBuy?.classList.remove('bg-blue-600', 'text-white');
        btnBuy?.classList.add('text-gray-400');

        btnSalva?.classList.remove('bg-blue-600');
        btnSalva?.classList.add('bg-red-600', 'text-white');
        if (btnSalva) btnSalva.textContent = "Salva Vendita (Chiudi Posizione)";

        // --- Caricamento dati per la vendita ---
        try {
            const uid = (window as any).UTENTE_ID || "C206BEC1-A5DE-4896-8472-3FF512635B3A";
            const response = await fetch(`/api/portafoglio/my-assets/${uid}`);

            if (response.ok) {
                // Assicurati che cacheMieiAssets sia dichiarata globalmente nel file
                (window as any).cacheMieiAssets = await response.json();
                console.log("Assets caricati per vendita:", (window as any).cacheMieiAssets);
            } else {
                console.error("Errore nel caricamento degli asset personali");
            }
        } catch (error) {
            console.error("Errore fetch vendita:", error);
        }
    }
}


document.addEventListener('DOMContentLoaded', () => {
    const inputRicerca = document.getElementById('filtro-ricerca');
    const selectTipo = document.getElementById('filtro-tipo');

    if (inputRicerca) {
        inputRicerca.addEventListener('input', (e) => {
            applicaFiltriStoria();
        });
    }

    if (selectTipo) {
        selectTipo.addEventListener('change', () => {
            applicaFiltriStoria();
        });
    }
});

// --- ESPOSIZIONE GLOBALE (Sintassi compatibile) ---
(window as any)["caricaPortafoglio"] = caricaPortafoglio;
(window as any)["salvaTransazioneModale"] = salvaTransazioneModale;
(window as any)["apriModale"] = () => {
    const m = document.getElementById('modal-transazione');
    if (m) m.classList.remove('hidden');
};
(window as any)["chiudiModale"] = () => {
    const m = document.getElementById('modal-transazione');
    if (m) m.classList.add('hidden');
};
(window as any)["eseguiRicerca"] = eseguiRicerca;
(window as any)["selezionaTitolo"] = selezionaTitolo;
(window as any)["chiediAI"] = chiediAI;
(window as any)["appendMessage"] = appendMessage;
(window as any)["mostraBenvenutoStatico"] = mostraBenvenutoStatico;
(window as any)["eliminaTrasazione"] = eliminaTrasazione;
(window as any)["preparaModificaTransazione"] = preparaModificaTransazione;
(window as any)["setTipoOperazione"] = setTipoOperazione;
(window as any)["apriModaleNuova"] = apriModaleNuova;
(window as any).applicaFiltriStoria = applicaFiltriStoria;

document.getElementById('chat-input')?.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') chiediAI();
});

document.getElementById('btn-chiedi-ai')?.addEventListener('click', chiediAI);
