using AnalistaFinanziarioIA.Core.Interfaces;
using AnalistaFinanziarioIA.Infrastructure.Data;
using AnalistaFinanziarioIA.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AnalistaFinanziarioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<ITitoloRepository, TitoloRepository>();
builder.Services.AddScoped<IQuotazioneRepository, QuotazioneRepository>();
builder.Services.AddScoped<IAnalisiRepository, AnalisiRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
