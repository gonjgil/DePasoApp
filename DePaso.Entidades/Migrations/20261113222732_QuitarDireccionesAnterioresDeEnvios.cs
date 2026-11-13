using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DePaso.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class QuitarDireccionesAnterioresDeEnvios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinoDir",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenDir",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoDir",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenDir",
                table: "Envios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinoDir",
                table: "Viajes",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenDir",
                table: "Viajes",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinoDir",
                table: "Envios",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenDir",
                table: "Envios",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
