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
    public DbSet<AnalisiFinanziaria> Analisi { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Titolo>(entity =>
        {
            entity.ToTable("Titoli");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Simbolo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Settore).HasMaxLength(100);
            entity.Property(e => e.Mercato).HasMaxLength(100);
            entity.HasIndex(e => e.Simbolo).IsUnique();
        });

        modelBuilder.Entity<QuotazioneStorica>(entity =>
        {
            entity.ToTable("QuotazioniStoriche");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrezzoApertura).HasPrecision(18, 4);
            entity.Property(e => e.PrezzoChiusura).HasPrecision(18, 4);
            entity.Property(e => e.PrezzoMassimo).HasPrecision(18, 4);
            entity.Property(e => e.PrezzoMinimo).HasPrecision(18, 4);
            entity.HasOne(e => e.Titolo)
                  .WithMany(t => t.QuotazioniStoriche)
                  .HasForeignKey(e => e.TitoloId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.TitoloId, e.Data }).IsUnique();
        });

        modelBuilder.Entity<AnalisiFinanziaria>(entity =>
        {
            entity.ToTable("Analisi");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TipoAnalisi).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Risultato).IsRequired();
            entity.Property(e => e.Raccomandazione).HasMaxLength(50);
            entity.Property(e => e.PrezzoTarget).HasPrecision(18, 4);
            entity.HasOne(e => e.Titolo)
                  .WithMany(t => t.Analisi)
                  .HasForeignKey(e => e.TitoloId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
