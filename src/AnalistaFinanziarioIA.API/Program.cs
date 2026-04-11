using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Core.Services;
using AnalistaFinanziarioIA.Core.Validators;
using AnalistaFinanziarioIA.Infrastructure.Data;
using AnalistaFinanziarioIA.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// 1. Registra i servizi dei Controller
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Registra tutti i validatori presenti nell'assembly dove si trova TitoloCreateValidator
builder.Services.AddValidatorsFromAssemblyContaining<TitoloCreateValidator>();

// Database
builder.Services.AddDbContext<AnalistaFinanziarioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrazione dei Servizi (Business Logic)
builder.Services.AddScoped<IValutaService, ValutaService>();
builder.Services.AddScoped<PortafoglioService>();


// Repositories
builder.Services.AddScoped<ITitoloRepository, TitoloRepository>();
builder.Services.AddScoped<IQuotazioneRepository, QuotazioneRepository>();
builder.Services.AddScoped<IAnalisiRepository, AnalisiRepository>();
builder.Services.AddScoped<IPortafoglioRepository, PortafoglioRepository>();

builder.Services.AddHttpClient();

// Registra il Client HTTP per il servizio quotazioni
// 2. Registriamo il Service risolvendo manualmente le dipendenze
builder.Services.AddScoped<IQuotazioneService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var repo = sp.GetRequiredService<IQuotazioneRepository>();
    var titoloRepo = sp.GetRequiredService<ITitoloRepository>();
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["AlphaVantage:ApiKey"] ?? "demo";

    return new QuotazioneService(httpClient, repo, titoloRepo, apiKey);
});

builder.Services.AddScoped<ITitoloService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["AlphaVantage:ApiKey"] ?? "demo";

    return new TitoloService(httpClient, apiKey);
});

// Configurazione Kernel (IA)
var openAiConfig = builder.Configuration.GetSection("OpenAI");
string apiKey = openAiConfig["ApiKey"] ?? "";
string modelId = openAiConfig["ModelId"] ?? "gpt-4o";
if (string.IsNullOrEmpty(apiKey))
{
    // Questo ti avvisa se ti sei dimenticato di impostarla!
    throw new Exception("Attenzione: OpenAI ApiKey non configurata in appsettings.json");
}

builder.Services.AddKernel()
                .AddOpenAIChatCompletion(modelId, apiKey);

// Importante: Questo dice all'IA di chiamare le funzioni C# automaticamente se servono
builder.Services.Configure<OpenAIPromptExecutionSettings>(settings =>
{
    settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- AGGIUNGI QUESTE RIGHE ---
app.UseDefaultFiles(); // Cerca automaticamente index.html
app.UseStaticFiles();  // Abilita il servizio dei file nella cartella wwwroot
//

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
