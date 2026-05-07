using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSegurancaLgpdEspera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UsuBloqueadoAte",
                table: "Usuario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuTentativasFalhas",
                table: "Usuario",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UsuTotpAtivo",
                table: "Usuario",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsuTotpSecret",
                table: "Usuario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgeAcessoToken",
                table: "Agendamento",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Popular agendamentos pré-existentes com tokens únicos antes do índice
            // unique. Sem isso, todos teriam Guid.Empty e o índice falharia.
            // Decisão por provider via ActiveProvider.
            if (migrationBuilder.ActiveProvider != null
                && migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(@"
                    UPDATE Agendamento
                    SET AgeAcessoToken =
                        substr(lower(hex(randomblob(16))), 1, 8) || '-' ||
                        substr(lower(hex(randomblob(16))), 1, 4) || '-' ||
                        substr(lower(hex(randomblob(16))), 1, 4) || '-' ||
                        substr(lower(hex(randomblob(16))), 1, 4) || '-' ||
                        substr(lower(hex(randomblob(16))), 1, 12)
                    WHERE AgeAcessoToken = '00000000-0000-0000-0000-000000000000';");
            }
            else
            {
                migrationBuilder.Sql(@"
                    UPDATE Agendamento SET AgeAcessoToken = NEWID()
                    WHERE AgeAcessoToken = '00000000-0000-0000-0000-000000000000';");
            }

            migrationBuilder.CreateTable(
                name: "Cupom",
                columns: table => new
                {
                    CupId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    CupCodigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CupTipo = table.Column<int>(type: "INTEGER", nullable: false),
                    CupValor = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    CupValidoDe = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CupValidoAte = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CupUsosMaximos = table.Column<int>(type: "INTEGER", nullable: false),
                    CupUsosFeitos = table.Column<int>(type: "INTEGER", nullable: false),
                    CupAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CupCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupom", x => x.CupId);
                    table.ForeignKey(
                        name: "FK_Cupom_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListaEspera",
                columns: table => new
                {
                    LesId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_SerId = table.Column<int>(type: "INTEGER", nullable: false),
                    LesDataDesejada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LesClienteNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LesClienteTelefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    LesClienteEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    LesObservacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LesNotificado = table.Column<bool>(type: "INTEGER", nullable: false),
                    LesNotificadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LesCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaEspera", x => x.LesId);
                    table.ForeignKey(
                        name: "FK_ListaEspera_Servico_R_SerId",
                        column: x => x.R_SerId,
                        principalTable: "Servico",
                        principalColumn: "SerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListaEspera_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_AgeAcessoToken",
                table: "Agendamento",
                column: "AgeAcessoToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cupom_R_TenId_CupCodigo",
                table: "Cupom",
                columns: new[] { "R_TenId", "CupCodigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_R_SerId",
                table: "ListaEspera",
                column: "R_SerId");

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_R_TenId_LesDataDesejada_LesNotificado",
                table: "ListaEspera",
                columns: new[] { "R_TenId", "LesDataDesejada", "LesNotificado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cupom");

            migrationBuilder.DropTable(
                name: "ListaEspera");

            migrationBuilder.DropIndex(
                name: "IX_Agendamento_AgeAcessoToken",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "UsuBloqueadoAte",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UsuTentativasFalhas",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UsuTotpAtivo",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "UsuTotpSecret",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "AgeAcessoToken",
                table: "Agendamento");
        }
    }
}
