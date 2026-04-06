# AnalistaFinanziarioIA

**AnalistaFinanziarioIA** è un'applicazione di analisi finanziaria basata su intelligenza artificiale, sviluppata in C# con backend ASP.NET Core e database SQL Server.

## Struttura del Progetto

```
AnalistaFinanziarioIA/
├── src/
│   ├── AnalistaFinanziarioIA.Core/            # Modelli di dominio e interfacce
│   │   ├── Models/                            # Entità (Titolo, QuotazioneStorica, AnalisiFinanziaria)
│   │   └── Interfaces/                        # Repository interfaces
│   ├── AnalistaFinanziarioIA.Infrastructure/  # Accesso ai dati (EF Core + SQL Server)
│   │   ├── Data/                              # DbContext
│   │   └── Repositories/                      # Implementazioni dei repository
│   └── AnalistaFinanziarioIA.API/             # ASP.NET Core Web API
│       └── Controllers/                       # Controller REST
├── sql/
│   ├── schema.sql                             # Schema del database
│   └── seed.sql                               # Dati di esempio
└── AnalistaFinanziarioIA.sln
```

## Tecnologie

- **Linguaggio**: C# 12 / .NET 8
- **Framework Web**: ASP.NET Core 8 Web API
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (2019+)
- **Documentazione API**: Swagger / OpenAPI

## Prerequisiti

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019 o superiore (oppure SQL Server Express)

## Avvio Rapido

### 1. Configurazione del Database

Eseguire gli script SQL nella seguente sequenza:

```bash
# Creazione schema
sqlcmd -S localhost -i sql/schema.sql

# Inserimento dati di esempio (opzionale)
sqlcmd -S localhost -i sql/seed.sql
```

### 2. Configurazione Connection String

Aggiornare `src/AnalistaFinanziarioIA.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<server>;Database=AnalistaFinanziarioDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Build e Avvio

```bash
dotnet build
dotnet run --project src/AnalistaFinanziarioIA.API
```

L'API sarà disponibile su `https://localhost:7021` e `http://localhost:5051`.  
La documentazione Swagger è accessibile su `https://localhost:7021/swagger`.

## Endpoints API

| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| GET    | `/api/titoli` | Elenco tutti i titoli |
| GET    | `/api/titoli/{id}` | Dettaglio titolo per ID |
| GET    | `/api/titoli/simbolo/{simbolo}` | Ricerca per simbolo |
| POST   | `/api/titoli` | Crea un nuovo titolo |
| PUT    | `/api/titoli/{id}` | Aggiorna un titolo |
| DELETE | `/api/titoli/{id}` | Elimina un titolo |
| GET    | `/api/titoli/{id}/quotazioni` | Storico quotazioni |
| GET    | `/api/titoli/{id}/quotazioni/ultima` | Ultima quotazione |
| GET    | `/api/titoli/{id}/quotazioni/periodo` | Quotazioni per periodo |
| POST   | `/api/titoli/{id}/quotazioni` | Aggiunge una quotazione |
| POST   | `/api/titoli/{id}/quotazioni/batch` | Aggiunge più quotazioni |
| GET    | `/api/titoli/{id}/analisi` | Tutte le analisi |
| GET    | `/api/titoli/{id}/analisi/{aid}` | Dettaglio analisi |
| POST   | `/api/titoli/{id}/analisi` | Crea una nuova analisi |
| PUT    | `/api/titoli/{id}/analisi/{aid}` | Aggiorna un'analisi |
| DELETE | `/api/titoli/{id}/analisi/{aid}` | Elimina un'analisi |
