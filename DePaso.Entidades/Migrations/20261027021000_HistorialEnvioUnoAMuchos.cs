using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DePaso.Entidades.Migrations
{
    public partial class HistorialEnvioUnoAMuchos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__EnvioEst__D024E23E3313BBA8",
                table: "EnvioEstadoHistoria");

            migrationBuilder.CreateIndex(
                name: "UQ__EnvioEst__D024E23E3313BBA8",
                table: "EnvioEstadoHistoria",
                column: "EnvioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__EnvioEst__D024E23E3313BBA8",
                table: "EnvioEstadoHistoria");

            migrationBuilder.CreateIndex(
                name: "UQ__EnvioEst__D024E23E3313BBA8",
                table: "EnvioEstadoHistoria",
                column: "EnvioId",
                unique: true);
        }
    }
}
