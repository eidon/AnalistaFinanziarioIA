using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaFinanziarioIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfittoRealizzato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProfittoRealizzatoTotale",
                table: "AssetsPortafoglio",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfittoRealizzatoTotale",
                table: "AssetsPortafoglio");
        }
    }
}
