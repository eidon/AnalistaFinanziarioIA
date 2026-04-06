using AnalistaFinanziarioIA.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalistaFinanziarioIA.Infrastructure.Data;

public class AnalistaFinanziarioDbContext : DbContext
{
    public AnalistaFinanziarioDbContext(DbContextOptions<AnalistaFinanziarioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Titolo> Titoli { get; set; }
    public DbSet<QuotazioneStorica> QuotazioniStoriche { get; set; }
    public DbSet<AnalisiFinanziaria> AnalisiFinanziarie { get; set; }

    public DbSet<Utente> Utenti { get; set; }
    public DbSet<AssetPortafoglio> AssetsPortafoglio { get; set; }
    public DbSet<Transazione> Transazioni { get; set; }
    public DbSet<Dividendo> Dividendi { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. APPLICAZIONE AUTOMATICA PRECISIONE DECIMALE
        // Configurazione precisione decimale (fondamentale per la finanza!)
        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

        foreach (var property in decimalProperties)
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        // 2. CONFIGURAZIONE TABELLA TITOLI
        modelBuilder.Entity<Titolo>(entity =>
        {
            entity.ToTable("Titoli");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Simbolo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Settore).HasMaxLength(100);
            entity.Property(e => e.Mercato).HasMaxLength(100);
            entity.HasIndex(e => e.Simbolo).IsUnique();
            entity.Property(e => e.DataCreazione).HasDefaultValueSql("GETDATE()");
        });

        // 3. CONFIGURAZIONE QUOTAZIONI
        modelBuilder.Entity<QuotazioneStorica>(entity =>
        {
            entity.ToTable("QuotazioniStoriche");
            entity.HasKey(e => e.Id);
            // Nota: avendo il ciclo sopra, queste HasPrecision(18,4) diventano opzionali ma lasciarle non fa male
            entity.HasOne(e => e.Titolo)
                  .WithMany(t => t.QuotazioniStoriche)
                  .HasForeignKey(e => e.TitoloId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.TitoloId, e.Data }).IsUnique();
        });

        // 4. CONFIGURAZIONE ANALISI
        modelBuilder.Entity<AnalisiFinanziaria>(entity =>
        {
            entity.ToTable("Analisi");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TipoAnalisi).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Titolo)
                  .WithMany(t => t.Analisi)
                  .HasForeignKey(e => e.TitoloId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. RELAZIONI PORTAFOGLIO E MULTI-UTENTE
        modelBuilder.Entity<AssetPortafoglio>(entity =>
        {
            entity.ToTable("AssetsPortafoglio"); // Coerenza con le altre tabelle
            entity.HasOne<Utente>()
                  .WithMany(u => u.Assets)
                  .HasForeignKey(a => a.UtenteId)
                  .OnDelete(DeleteBehavior.Cascade); // Se elimini l'utente, elimini il suo portafoglio

            entity.HasOne(a => a.Titolo)
                  .WithMany()
                  .HasForeignKey(a => a.TitoloId)
                  .OnDelete(DeleteBehavior.Restrict); // Non vogliamo eliminare un Titolo di mercato se qualcuno lo ha in portafoglio
        });

        // 6. TRANSAZIONI E DIVIDENDI (Aggiungiamo il mapping esplicito delle tabelle)
        modelBuilder.Entity<Transazione>().ToTable("Transazioni");
        modelBuilder.Entity<Dividendo>().ToTable("Dividendi");
        modelBuilder.Entity<Utente>().ToTable("Utenti");

    }
}
