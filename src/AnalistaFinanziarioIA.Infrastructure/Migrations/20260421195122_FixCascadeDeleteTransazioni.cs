using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaFinanziarioIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteTransazioni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transazioni_AssetsPortafoglio_AssetPortafoglioId",
                table: "Transazioni");

            migrationBuilder.AddForeignKey(
                name: "FK_Transazioni_AssetsPortafoglio_AssetPortafoglioId",
                table: "Transazioni",
                column: "AssetPortafoglioId",
                principalTable: "AssetsPortafoglio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transazioni_AssetsPortafoglio_AssetPortafoglioId",
                table: "Transazioni");

            migrationBuilder.AddForeignKey(
                name: "FK_Transazioni_AssetsPortafoglio_AssetPortafoglioId",
                table: "Transazioni",
                column: "AssetPortafoglioId",
                principalTable: "AssetsPortafoglio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
