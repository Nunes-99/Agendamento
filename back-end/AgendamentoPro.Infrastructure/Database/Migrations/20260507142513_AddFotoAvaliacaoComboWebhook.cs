using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoAvaliacaoComboWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avaliacao",
                columns: table => new
                {
                    AvaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_AgeId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_CliId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvaToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    AvaNota = table.Column<int>(type: "INTEGER", nullable: true),
                    AvaComentario = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AvaCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvaRespondidoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AvaPublica = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avaliacao", x => x.AvaId);
                    table.ForeignKey(
                        name: "FK_Avaliacao_Agendamento_R_AgeId",
                        column: x => x.R_AgeId,
                        principalTable: "Agendamento",
                        principalColumn: "AgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Avaliacao_Cliente_R_CliId",
                        column: x => x.R_CliId,
                        principalTable: "Cliente",
                        principalColumn: "CliId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Avaliacao_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Combo",
                columns: table => new
                {
                    ComId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComNome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ComDescricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ComImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ComPrecoPromocional = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ComAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ComOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    ComCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combo", x => x.ComId);
                    table.ForeignKey(
                        name: "FK_Combo_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FotoAgendamento",
                columns: table => new
                {
                    FotId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_AgeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FotTipo = table.Column<int>(type: "INTEGER", nullable: false),
                    FotUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FotNomeOriginal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    FotContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FotTamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    FotCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotoAgendamento", x => x.FotId);
                    table.ForeignKey(
                        name: "FK_FotoAgendamento_Agendamento_R_AgeId",
                        column: x => x.R_AgeId,
                        principalTable: "Agendamento",
                        principalColumn: "AgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FotoAgendamento_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComboServico",
                columns: table => new
                {
                    ComServId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_ComId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_SerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboServico", x => x.ComServId);
                    table.ForeignKey(
                        name: "FK_ComboServico_Combo_R_ComId",
                        column: x => x.R_ComId,
                        principalTable: "Combo",
                        principalColumn: "ComId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboServico_Servico_R_SerId",
                        column: x => x.R_SerId,
                        principalTable: "Servico",
                        principalColumn: "SerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacao_AvaToken",
                table: "Avaliacao",
                column: "AvaToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacao_R_AgeId",
                table: "Avaliacao",
                column: "R_AgeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacao_R_CliId",
                table: "Avaliacao",
                column: "R_CliId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacao_R_TenId_AvaRespondidoEm",
                table: "Avaliacao",
                columns: new[] { "R_TenId", "AvaRespondidoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Combo_R_TenId_ComAtivo",
                table: "Combo",
                columns: new[] { "R_TenId", "ComAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboServico_R_ComId_R_SerId",
                table: "ComboServico",
                columns: new[] { "R_ComId", "R_SerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboServico_R_SerId",
                table: "ComboServico",
                column: "R_SerId");

            migrationBuilder.CreateIndex(
                name: "IX_FotoAgendamento_R_AgeId",
                table: "FotoAgendamento",
                column: "R_AgeId");

            migrationBuilder.CreateIndex(
                name: "IX_FotoAgendamento_R_TenId_R_AgeId",
                table: "FotoAgendamento",
                columns: new[] { "R_TenId", "R_AgeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avaliacao");

            migrationBuilder.DropTable(
                name: "ComboServico");

            migrationBuilder.DropTable(
                name: "FotoAgendamento");

            migrationBuilder.DropTable(
                name: "Combo");
        }
    }
}
