# AnalistaFinanziarioIA

**AnalistaFinanziarioIA** è un'applicazione di analisi finanziaria basata su intelligenza artificiale, sviluppata in C# con backend ASP.NET Core e database SQL Server. Grazie all'integrazione con **Microsoft Semantic Kernel** e **OpenAI GPT-4o**, l'applicazione offre un analista virtuale in grado di leggere il portafoglio dell'utente, recuperare prezzi di mercato in tempo reale e fornire analisi personalizzate.

## Struttura del Progetto

```
AnalistaFinanziarioIA/
├── src/
│   ├── AnalistaFinanziarioIA.Core/            # Modelli di dominio, interfacce e logica di business
│   │   ├── Models/                            # Entità (Titolo, QuotazioneStorica, AnalisiFinanziaria,
│   │   │                                      #         AssetPortafoglio, Transazione, Dividendo, Utente)
│   │   ├── Interfaces/                        # Interfacce dei repository e dei servizi
│   │   ├── DTOs/                              # Data Transfer Objects (input/output API)
│   │   ├── Services/                          # Servizi di business (Portafoglio, Quotazione, Titolo, Valuta)
│   │   └── Validators/                        # Validatori FluentValidation
│   ├── AnalistaFinanziarioIA.Infrastructure/  # Accesso ai dati (EF Core + SQL Server)
│   │   ├── Data/                              # DbContext
│   │   ├── Repositories/                      # Implementazioni dei repository
│   │   └── Migrations/                        # Migrazioni EF Core
│   └── AnalistaFinanziarioIA.API/             # ASP.NET Core Web API
│       ├── Controllers/                       # Controller REST (7 controller)
│       ├── Plugins/                           # Plugin Semantic Kernel (MercatoPlugin, PortafoglioPlugin)
│       ├── wwwroot/                           # Frontend statico (index.html)
│       └── Program.cs                         # Configurazione dell'applicazione e DI
├── sql/
│   ├── schema.sql                             # Schema del database
│   └── seed.sql                               # Dati di esempio
└── AnalistaFinanziarioIA.slnx
```

## Tecnologie

- **Linguaggio**: C# 12 / .NET 8
- **Framework Web**: ASP.NET Core 8 Web API
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (2019+)
- **Intelligenza Artificiale**: Microsoft Semantic Kernel + OpenAI GPT-4o
- **Prezzi di Mercato**: Alpha Vantage API
- **Ricerca Titoli**: Financial Modeling Prep (FMP) API
- **Tassi di Cambio**: ExchangeRate API
- **Validazione**: FluentValidation
- **Documentazione API**: Swagger / OpenAPI

## Prerequisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019 o superiore (oppure SQL Server Express)
- Chiave API [OpenAI](https://platform.openai.com/) (GPT-4o)
- Chiave API [Alpha Vantage](https://www.alphavantage.co/) (prezzi di mercato)
- Chiave API [Financial Modeling Prep](https://financialmodelingprep.com/) (ricerca titoli)
- Chiave API [ExchangeRate API](https://www.exchangerate-api.com/) (tassi di cambio)

## Avvio Rapido

### 1. Configurazione del Database

Eseguire gli script SQL nella seguente sequenza:

```bash
# Creazione schema
sqlcmd -S localhost -i sql/schema.sql

# Inserimento dati di esempio (opzionale)
sqlcmd -S localhost -i sql/seed.sql
```

### 2. Configurazione di `appsettings.json`

Aggiornare `src/AnalistaFinanziarioIA.API/appsettings.json` con le proprie credenziali:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<server>;Database=AnalistaFinanziarioDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "OpenAI": {
    "ApiKey": "<la-tua-chiave-openai>",
    "ModelId": "gpt-4o"
  },
  "AlphaVantage": {
    "ApiKey": "<la-tua-chiave-alphavantage>"
  },
  "FinancialModelingPrep": {
    "ApiKey": "<la-tua-chiave-fmp>"
  },
  "ExchangeRateApiKey": "<la-tua-chiave-exchangerate>"
}
```

> ⚠️ **Attenzione**: L'applicazione non si avvia se `OpenAI:ApiKey` non è configurata.

### 3. Build e Avvio

```bash
dotnet build
dotnet run --project src/AnalistaFinanziarioIA.API
```

L'API sarà disponibile su `https://localhost:7021` e `http://localhost:5051`.  
La documentazione Swagger è accessibile su `https://localhost:7021/swagger`.  
Il frontend statico è servito direttamente da `https://localhost:7021`.

## Endpoints API

### Titoli

| Metodo | Endpoint                          | Descrizione                                      |
|--------|-----------------------------------|--------------------------------------------------|
| GET    | `/api/titoli`                     | Elenco di tutti i titoli                         |
| GET    | `/api/titoli/{id}`                | Dettaglio titolo per ID                          |
| GET    | `/api/titoli/simbolo/{simbolo}`   | Ricerca titolo per simbolo                       |
| GET    | `/api/titoli/cerca?query={q}`     | Ricerca titoli per nome/simbolo (ricerca locale) |
| POST   | `/api/titoli`                     | Crea un nuovo titolo                             |
| PUT    | `/api/titoli/{id}`                | Aggiorna un titolo                               |
| DELETE | `/api/titoli/{id}`                | Elimina un titolo                                |

### Ricerca Titoli tramite FMP

| Metodo | Endpoint                              | Descrizione                                             |
|--------|---------------------------------------|---------------------------------------------------------|
| GET    | `/api/titolo/cerca?q={q}&tipo={tipo}` | Cerca titoli tramite Financial Modeling Prep (per nome o ISIN) |

### Quotazioni

| Metodo | Endpoint                                          | Descrizione                                     |
|--------|---------------------------------------------------|-------------------------------------------------|
| GET    | `/api/titoli/{titoloId}/quotazioni`               | Storico quotazioni del titolo                   |
| GET    | `/api/titoli/{titoloId}/quotazioni/ultima`        | Ultima quotazione disponibile                   |
| GET    | `/api/titoli/{titoloId}/quotazioni/periodo`       | Quotazioni in un intervallo di date             |
| POST   | `/api/titoli/{titoloId}/quotazioni`               | Aggiunge una quotazione                         |
| POST   | `/api/titoli/{titoloId}/quotazioni/batch`         | Aggiunge più quotazioni in blocco               |
| POST   | `/api/titoli/{titoloId}/quotazioni/aggiorna`      | Aggiorna il prezzo tramite Alpha Vantage        |

### Analisi Finanziarie

| Metodo | Endpoint                                     | Descrizione                        |
|--------|----------------------------------------------|------------------------------------|
| GET    | `/api/titoli/{titoloId}/analisi`             | Tutte le analisi del titolo        |
| GET    | `/api/titoli/{titoloId}/analisi/{id}`        | Dettaglio analisi                  |
| POST   | `/api/titoli/{titoloId}/analisi`             | Crea una nuova analisi             |
| PUT    | `/api/titoli/{titoloId}/analisi/{id}`        | Aggiorna un'analisi                |
| DELETE | `/api/titoli/{titoloId}/analisi/{id}`        | Elimina un'analisi                 |

### Portafoglio

| Metodo | Endpoint                                | Descrizione                                                |
|--------|-----------------------------------------|------------------------------------------------------------|
| GET    | `/api/portafoglio/dashboard/{utenteId}` | Dashboard completa con totali, asset e tassi di cambio     |
| POST   | `/api/portafoglio/transazione`          | Registra un'operazione di acquisto                         |
| GET    | `/api/portafoglio/utente/{utenteId}`    | Storia delle transazioni dell'utente (raggruppate per mese)|

### Transazioni

| Metodo | Endpoint                               | Descrizione                        |
|--------|----------------------------------------|------------------------------------|
| GET    | `/api/transazioni/utente/{utenteId}`   | Storia transazioni (con filtro opzionale per simbolo) |
| DELETE | `/api/transazioni/{id}`                | Elimina una transazione            |
| PUT    | `/api/transazioni/{id}`                | Modifica una transazione esistente |

### Analista AI

| Metodo | Endpoint                                        | Descrizione                                                         |
|--------|-------------------------------------------------|---------------------------------------------------------------------|
| POST   | `/api/chat/chiedi?utenteId={guid}`              | Pone una domanda all'analista AI (body: stringa con la domanda)     |

## Funzionalità AI

L'endpoint `/api/chat/chiedi` sfrutta **Microsoft Semantic Kernel** per orchestrare un ciclo di ragionamento con tool-calling automatico. L'IA ha accesso a due plugin:

- **PortafoglioPlugin**: recupera la composizione del portafoglio dell'utente dal database, inclusi PMC (con commissioni e tasse incluse) e note strategiche sulle transazioni.
- **MercatoPlugin**: recupera prezzi in tempo reale tramite Alpha Vantage, aggiorna i prezzi nel database, ottiene tassi di cambio USD/EUR tramite ExchangeRate API e converte valute.

L'analista risponde in italiano con analisi P&L, suggerimenti strategici basati sulle note dell'utente e avvisi in caso di incongruenze nei dati.
