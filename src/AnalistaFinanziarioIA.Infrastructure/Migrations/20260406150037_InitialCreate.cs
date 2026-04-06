using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaFinanziarioIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Titoli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Simbolo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Settore = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mercato = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titoli", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValutaBase = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Analisi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitoloId = table.Column<int>(type: "int", nullable: false),
                    DataAnalisi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoAnalisi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Risultato = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrezzoTarget = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Raccomandazione = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analisi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Analisi_Titoli_TitoloId",
                        column: x => x.TitoloId,
                        principalTable: "Titoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotazioniStoriche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitoloId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrezzoApertura = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrezzoChiusura = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrezzoMassimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrezzoMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotazioniStoriche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotazioniStoriche_Titoli_TitoloId",
                        column: x => x.TitoloId,
                        principalTable: "Titoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetsPortafoglio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitoloId = table.Column<int>(type: "int", nullable: false),
                    QuantitaTotale = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrezzoMedioCarico = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetsPortafoglio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetsPortafoglio_Titoli_TitoloId",
                        column: x => x.TitoloId,
                        principalTable: "Titoli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetsPortafoglio_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dividendi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetPortafoglioId = table.Column<int>(type: "int", nullable: false),
                    ImportoLordo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TasseTrattenute = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Valuta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividendi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dividendi_AssetsPortafoglio_AssetPortafoglioId",
                        column: x => x.AssetPortafoglioId,
                        principalTable: "AssetsPortafoglio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetPortafoglioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Quantita = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrezzoUnita = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Commissioni = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TassoCambio = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transazioni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transazioni_AssetsPortafoglio_AssetPortafoglioId",
                        column: x => x.AssetPortafoglioId,
                        principalTable: "AssetsPortafoglio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analisi_TitoloId",
                table: "Analisi",
                column: "TitoloId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsPortafoglio_TitoloId",
                table: "AssetsPortafoglio",
                column: "TitoloId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetsPortafoglio_UtenteId",
                table: "AssetsPortafoglio",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Dividendi_AssetPortafoglioId",
                table: "Dividendi",
                column: "AssetPortafoglioId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotazioniStoriche_TitoloId_Data",
                table: "QuotazioniStoriche",
                columns: new[] { "TitoloId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Titoli_Simbolo",
                table: "Titoli",
                column: "Simbolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_AssetPortafoglioId",
                table: "Transazioni",
                column: "AssetPortafoglioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analisi");

            migrationBuilder.DropTable(
                name: "Dividendi");

            migrationBuilder.DropTable(
                name: "QuotazioniStoriche");

            migrationBuilder.DropTable(
                name: "Transazioni");

            migrationBuilder.DropTable(
                name: "AssetsPortafoglio");

            migrationBuilder.DropTable(
                name: "Titoli");

            migrationBuilder.DropTable(
                name: "Utenti");
        }
    }
}
