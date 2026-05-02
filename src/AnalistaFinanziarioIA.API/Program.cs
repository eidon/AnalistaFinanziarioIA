using AnalistaFinanziarioIA.API.BackgroundServices;
using AnalistaFinanziarioIA.API.Plugins;
using AnalistaFinanziarioIA.API.Services;
using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Services;
using AnalistaFinanziarioIA.Core.Validators;
using AnalistaFinanziarioIA.Infrastructure.Data;
using AnalistaFinanziarioIA.Infrastructure.Repositories;
using AnalistaFinanziarioIA.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// 1. INFRASTRUTTURA BASE
// ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ─────────────────────────────────────────────
// 2. DATABASE
// ─────────────────────────────────────────────
builder.Services.AddDbContext<AnalistaFinanziarioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────
// 3. REPOSITORIES
// ─────────────────────────────────────────────
builder.Services.AddScoped<ITitoloRepository, TitoloRepository>();
builder.Services.AddScoped<IQuotazioneRepository, QuotazioneRepository>();
builder.Services.AddScoped<IAnalisiRepository, AnalisiRepository>();
builder.Services.AddScoped<IPortafoglioRepository, PortafoglioRepository>();

// ─────────────────────────────────────────────
// 4. SERVIZI DI BUSINESS LOGIC
// ─────────────────────────────────────────────


// Handler per Yahoo Finance (Usa SocketsHttpHandler per gestire il ciclo di vita della connessione)
builder.Services.AddHttpClient<IYahooFinanceService, YahooFinanceService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Questo sostituisce il vecchio HttpClientHandler
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        // Bypass SSL (quello che facevi prima)
        RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
        // Forza protocolli sicuri
        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
    },
    // Fondamentale per Yahoo: chiude la connessione dopo 1 minuto per evitare errori di timeout/rifiuto
    PooledConnectionLifetime = TimeSpan.FromMinutes(1)
});


// Configuriamo un handler comune per il bypass SSL in Docker
var sslBypassHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

builder.Services.AddHttpClient<IValutaService, ValutaService>()
    .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);

// AGGIUNGI QUESTO: Registriamo un client per i servizi generici che non hanno un'interfaccia dedicata
builder.Services.AddHttpClient("SslBypassClient")
    .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);

builder.Services.AddScoped<PortafoglioService>();
builder.Services.AddScoped<ITransazioneService, TransazioneService>();


builder.Services.AddScoped<IQuotazioneService>(sp =>
{
    // Usiamo il client con bypass SSL
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SslBypassClient");
    var repo = sp.GetRequiredService<IQuotazioneRepository>();
    var titoloRepo = sp.GetRequiredService<ITitoloRepository>();
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["AlphaVantage:ApiKey"] ?? "demo";
    return new QuotazioneService(httpClient, repo, titoloRepo, apiKey);
});

builder.Services.AddScoped<ITitoloService>(sp =>
{
    // Usiamo il client con bypass SSL
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SslBypassClient");
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["FinancialModelingPrep:ApiKey"] ?? "demo";
    return new TitoloService(httpClient, apiKey);
});


// ─────────────────────────────────────────────
// 5. VALIDATORI
// ─────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<TitoloCreateValidator>();

// ─────────────────────────────────────────────
// 6. PLUGIN IA
// Scoped: risolvono dipendenze Scoped (repository, ecc.)
// ─────────────────────────────────────────────
builder.Services.AddScoped<PortafoglioPlugin>();
builder.Services.AddScoped<MercatoPlugin>();

// WebSearchPlugin è stateless → Singleton
builder.Services.AddSingleton<WebSearchPlugin>(sp =>
{
    var key = builder.Configuration["Tavily:ApiKey"]
              ?? throw new InvalidOperationException("Tavily:ApiKey non configurata.");
    // Usa il client col bypass SSL
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SslBypassClient");
    return new WebSearchPlugin(key, httpClient); // Assicurati che il costruttore del plugin lo accetti
});

// ─────────────────────────────────────────────
// 7. SEMANTIC KERNEL
// Kernel come Scoped per risolvere plugin Scoped correttamente.
// I plugin vengono registrati QUI, una sola volta per scope.
// ─────────────────────────────────────────────
var openAiConfig = builder.Configuration.GetSection("OpenAI");
var aiApiKey = openAiConfig["ApiKey"]
               ?? throw new InvalidOperationException("OpenAI:ApiKey non configurata in appsettings.json.");
var modelId = openAiConfig["ModelId"] ?? "gpt-4o";

builder.Services.AddScoped<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(modelId, aiApiKey);

    // Risolviamo i plugin dal DI container (già registrati sopra)
    var portafoglioPlugin = sp.GetRequiredService<PortafoglioPlugin>();
    var mercatoPlugin = sp.GetRequiredService<MercatoPlugin>();
    var webSearchPlugin = sp.GetRequiredService<WebSearchPlugin>();

    kernelBuilder.Plugins.AddFromObject(portafoglioPlugin, "PortafoglioManager");
    kernelBuilder.Plugins.AddFromObject(mercatoPlugin, "MercatoManager");
    kernelBuilder.Plugins.AddFromObject(webSearchPlugin, "WebSearch");

    return kernelBuilder.Build();
});

// Settings condivisi per l'auto-invocazione dei tool (Singleton: sono immutabili)
builder.Services.AddSingleton(new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
});

// ─────────────────────────────────────────────
// 8. GESTIONE SESSIONI CHAT
// Singleton: gestisce stato in-memory cross-request.
// In produzione sostituire con IDistributedCache (Redis).
// ─────────────────────────────────────────────
builder.Services.AddSingleton<IChatSessionService, InMemoryChatSessionService>();

// ─────────────────────────────────────────────
// 9. BACKGROUND SERVICES
// ─────────────────────────────────────────────
builder.Services.AddHostedService<PrezzoAggiornamentoService>();
builder.Services.AddHostedService<SessionCleanupService>();

// ─────────────────────────────────────────────
// APP PIPELINE
// ─────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();   // Cerca automaticamente index.html in wwwroot
app.UseStaticFiles();    // Abilita servizio file statici

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();