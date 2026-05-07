using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRecorrenciaPacoteFidelidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendamentoRecorrente",
                columns: table => new
                {
                    RecId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_CliId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_SerId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_RecursoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecDiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    RecHoraInicio = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RecFrequencia = table.Column<int>(type: "INTEGER", nullable: false),
                    RecQuantidadeOcorrencias = table.Column<int>(type: "INTEGER", nullable: false),
                    RecDataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecAtivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentoRecorrente", x => x.RecId);
                    table.ForeignKey(
                        name: "FK_AgendamentoRecorrente_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PacotePrePago",
                columns: table => new
                {
                    PctId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_SerId = table.Column<int>(type: "INTEGER", nullable: false),
                    PctNome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PctQuantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    PctPreco = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PctValidadeDias = table.Column<int>(type: "INTEGER", nullable: false),
                    PctAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    PctCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacotePrePago", x => x.PctId);
                    table.ForeignKey(
                        name: "FK_PacotePrePago_Servico_R_SerId",
                        column: x => x.R_SerId,
                        principalTable: "Servico",
                        principalColumn: "SerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PacotePrePago_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PontosFidelidade",
                columns: table => new
                {
                    PtsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_CliId = table.Column<int>(type: "INTEGER", nullable: false),
                    PtsSaldo = table.Column<int>(type: "INTEGER", nullable: false),
                    PtsAtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontosFidelidade", x => x.PtsId);
                    table.ForeignKey(
                        name: "FK_PontosFidelidade_Cliente_R_CliId",
                        column: x => x.R_CliId,
                        principalTable: "Cliente",
                        principalColumn: "CliId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PontosFidelidade_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaldoPacote",
                columns: table => new
                {
                    SaldId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_CliId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_PctId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaldQuantidadeRestante = table.Column<int>(type: "INTEGER", nullable: false),
                    SaldExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SaldCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldoPacote", x => x.SaldId);
                    table.ForeignKey(
                        name: "FK_SaldoPacote_PacotePrePago_R_PctId",
                        column: x => x.R_PctId,
                        principalTable: "PacotePrePago",
                        principalColumn: "PctId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaldoPacote_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoRecorrente_R_TenId_RecAtivo",
                table: "AgendamentoRecorrente",
                columns: new[] { "R_TenId", "RecAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_PacotePrePago_R_SerId",
                table: "PacotePrePago",
                column: "R_SerId");

            migrationBuilder.CreateIndex(
                name: "IX_PacotePrePago_R_TenId_PctAtivo",
                table: "PacotePrePago",
                columns: new[] { "R_TenId", "PctAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_PontosFidelidade_R_CliId",
                table: "PontosFidelidade",
                column: "R_CliId");

            migrationBuilder.CreateIndex(
                name: "IX_PontosFidelidade_R_TenId_R_CliId",
                table: "PontosFidelidade",
                columns: new[] { "R_TenId", "R_CliId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaldoPacote_R_PctId",
                table: "SaldoPacote",
                column: "R_PctId");

            migrationBuilder.CreateIndex(
                name: "IX_SaldoPacote_R_TenId_R_CliId",
                table: "SaldoPacote",
                columns: new[] { "R_TenId", "R_CliId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentoRecorrente");

            migrationBuilder.DropTable(
                name: "PontosFidelidade");

            migrationBuilder.DropTable(
                name: "SaldoPacote");

            migrationBuilder.DropTable(
                name: "PacotePrePago");
        }
    }
}
