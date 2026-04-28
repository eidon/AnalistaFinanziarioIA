using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnalistaFinanziarioIA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RinominaTipoInTipoOperazione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "Transazioni",
                newName: "TipoOperazione");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipoOperazione",
                table: "Transazioni",
                newName: "Tipo");
        }
    }
}
