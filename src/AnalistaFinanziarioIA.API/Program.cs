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
// 1. SERVIZI DI INFRASTRUTTURA BASE
// ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DATABASE
builder.Services.AddDbContext<AnalistaFinanziarioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// REPOSITORIES
builder.Services.AddScoped<ITitoloRepository, TitoloRepository>();
builder.Services.AddScoped<IQuotazioneRepository, QuotazioneRepository>();
builder.Services.AddScoped<IAnalisiRepository, AnalisiRepository>();
builder.Services.AddScoped<IPortafoglioRepository, PortafoglioRepository>();

// ─────────────────────────────────────────────
// 2. CONFIGURAZIONE HTTP CLIENT & SICUREZZA SSL
// ─────────────────────────────────────────────

// Helper locale per gestire il bypass SSL condizionale
HttpMessageHandler GetConditionalHandler(IServiceProvider sp)
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var handler = new HttpClientHandler();

    if (env.IsDevelopment())
    {
        // Bypass attivo SOLO in ambiente di sviluppo/Docker
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
}

// YAHOO FINANCE: Configurazione specifica con SocketsHttpHandler
builder.Services.AddHttpClient<IYahooFinanceService, YahooFinanceService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var handler = new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(1)
    };

    if (env.IsDevelopment())
    {
        handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
    }
    return handler;
});

// CLIENT GENERICO protetto per servizi esterni (AlphaVantage, FMP, Tavily)
builder.Services.AddHttpClient("SecureApiClient")
    .ConfigurePrimaryHttpMessageHandler(sp => GetConditionalHandler(sp));

// VALUTA SERVICE
builder.Services.AddHttpClient<IValutaService, ValutaService>()
    .ConfigurePrimaryHttpMessageHandler(sp => GetConditionalHandler(sp));


// ─────────────────────────────────────────────
// 3. REGISTRAZIONE SERVIZI DI BUSINESS LOGIC
// ─────────────────────────────────────────────
builder.Services.AddScoped<PortafoglioService>();
builder.Services.AddScoped<ITransazioneService, TransazioneService>();

builder.Services.AddScoped<IQuotazioneService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SecureApiClient");
    var repo = sp.GetRequiredService<IQuotazioneRepository>();
    var titoloRepo = sp.GetRequiredService<ITitoloRepository>();
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["AlphaVantage:ApiKey"] ?? "demo";
    return new QuotazioneService(httpClient, repo, titoloRepo, apiKey);
});

builder.Services.AddScoped<ITitoloService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SecureApiClient");
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["FinancialModelingPrep:ApiKey"] ?? "demo";
    return new TitoloService(httpClient, apiKey);
});

// VALIDATORI
builder.Services.AddValidatorsFromAssemblyContaining<TitoloCreateValidator>();

// ─────────────────────────────────────────────
// 4. INTELLIGENZA ARTIFICIALE (SEMANTIC KERNEL)
// ─────────────────────────────────────────────
builder.Services.AddScoped<PortafoglioPlugin>();
builder.Services.AddScoped<MercatoPlugin>();

builder.Services.AddSingleton<WebSearchPlugin>(sp =>
{
    var key = builder.Configuration["Tavily:ApiKey"] ?? throw new InvalidOperationException("Tavily:ApiKey mancante.");
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SecureApiClient");
    return new WebSearchPlugin(key, httpClient);
});

builder.Services.AddScoped<Kernel>(sp =>
{
    var openAiConfig = builder.Configuration.GetSection("OpenAI");
    var aiApiKey = openAiConfig["ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey mancante.");
    var modelId = openAiConfig["ModelId"] ?? "gpt-4o";

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(modelId, aiApiKey);

    // Registrazione Plugin
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<PortafoglioPlugin>(), "PortafoglioManager");
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<MercatoPlugin>(), "MercatoManager");
    kernelBuilder.Plugins.AddFromObject(sp.GetRequiredService<WebSearchPlugin>(), "WebSearch");

    return kernelBuilder.Build();
});

builder.Services.AddSingleton(new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
});

builder.Services.AddSingleton<IChatSessionService, InMemoryChatSessionService>();

// ─────────────────────────────────────────────
// 5. BACKGROUND SERVICES
// ─────────────────────────────────────────────
builder.Services.AddHostedService<PrezzoAggiornamentoService>();
builder.Services.AddHostedService<SessionCleanupService>();

// ─────────────────────────────────────────────
// 6. PIPELINE DI ESECUZIONE (MIDDLEWARE)
// ─────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Sicurezza: Reindirizza sempre su HTTPS tranne in locale se non configurato
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();