using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DePaso.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Usuarios__3214EC07E7F20049", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OrigenDir = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    OrigenLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    OrigenLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoDir = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    DestinoLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    FechaLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Tamanio = table.Column<byte>(type: "tinyint", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    RowVersion = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Envios__3214EC07F23EEC5D", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Envio_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Viajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OrigenDir = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    OrigenLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    OrigenLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoDir = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    DestinoLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    HoraSalida = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraLlegada = table.Column<TimeOnly>(type: "time", nullable: false),
                    MaxPaquetes = table.Column<int>(type: "int", nullable: false),
                    PesoMaxKg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DesvioMaxKm = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    DistanciaKm = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    DuracionMin = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Viajes__3214EC07838E3B0F", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Viaje_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnvioEstadoHistoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnvioId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<byte>(type: "tinyint", nullable: false),
                    EstadoNuevo = table.Column<byte>(type: "tinyint", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EnvioEst__3214EC07F52EABDD", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvioEstadoHistoria_Envio",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnvioId = table.Column<int>(type: "int", nullable: false),
                    ViajeId = table.Column<int>(type: "int", nullable: false),
                    FechaViaje = table.Column<DateOnly>(type: "date", nullable: false),
                    DesvioKm = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    TiempoExtraMin = table.Column<int>(type: "int", nullable: false),
                    Ganancia = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    AsignadoEn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Asignaci__3214EC07C705ED66", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignacion_Envio",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asignacion_Viaje",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Paradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ViajeId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Paradas__3214EC077DADEF59", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parada_Viaje",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ViajeDia",
                columns: table => new
                {
                    ViajeId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViajeDia", x => new { x.ViajeId, x.DiaSemana });
                    table.ForeignKey(
                        name: "FK_ViajeDia_Viaje",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_ViajeId",
                table: "Asignaciones",
                column: "ViajeId");

            migrationBuilder.CreateIndex(
                name: "UQ__Asignaci__D024E23E16902D18",
                table: "Asignaciones",
                column: "EnvioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__EnvioEst__D024E23E3313BBA8",
                table: "EnvioEstadoHistoria",
                column: "EnvioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envios_UsuarioId",
                table: "Envios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Paradas_ViajeId",
                table: "Paradas",
                column: "ViajeId");

            migrationBuilder.CreateIndex(
                name: "UQ__Usuarios__A9D10534338279AE",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_UsuarioId",
                table: "Viajes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "EnvioEstadoHistoria");

            migrationBuilder.DropTable(
                name: "Paradas");

            migrationBuilder.DropTable(
                name: "ViajeDia");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropTable(
                name: "Viajes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
