using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DePaso.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class DividirDirecciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinoAltura",
                table: "Viajes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DestinoCalle",
                table: "Viajes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinoDepartamento",
                table: "Viajes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinoLocalidad",
                table: "Viajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinoPiso",
                table: "Viajes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinoProvincia",
                table: "Viajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrigenAltura",
                table: "Viajes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OrigenCalle",
                table: "Viajes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenDepartamento",
                table: "Viajes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenLocalidad",
                table: "Viajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenPiso",
                table: "Viajes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenProvincia",
                table: "Viajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Altura",
                table: "Paradas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Calle",
                table: "Paradas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Paradas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localidad",
                table: "Paradas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Piso",
                table: "Paradas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                table: "Paradas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DestinoAltura",
                table: "Envios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DestinoCalle",
                table: "Envios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinoDepartamento",
                table: "Envios",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinoLocalidad",
                table: "Envios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinoPiso",
                table: "Envios",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinoProvincia",
                table: "Envios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrigenAltura",
                table: "Envios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OrigenCalle",
                table: "Envios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenDepartamento",
                table: "Envios",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenLocalidad",
                table: "Envios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrigenPiso",
                table: "Envios",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenProvincia",
                table: "Envios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinoAltura",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoCalle",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoDepartamento",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoLocalidad",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoPiso",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "DestinoProvincia",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenAltura",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenCalle",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenDepartamento",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenLocalidad",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenPiso",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "OrigenProvincia",
                table: "Viajes");

            migrationBuilder.DropColumn(
                name: "Altura",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Calle",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Localidad",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Piso",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Provincia",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "DestinoAltura",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "DestinoCalle",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "DestinoDepartamento",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "DestinoLocalidad",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "DestinoPiso",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "DestinoProvincia",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenAltura",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenCalle",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenDepartamento",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenLocalidad",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenPiso",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "OrigenProvincia",
                table: "Envios");
        }
    }
}
