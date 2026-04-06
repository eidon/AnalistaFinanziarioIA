using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaFinanziarioIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoTipoTitoloATitoli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Categoria",
                table: "Titoli",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Titoli");
        }
    }
}
