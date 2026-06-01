using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanoAssinaturaFatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plano",
                columns: table => new
                {
                    PlnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlnNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PlnDescricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PlnPreco = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PlnLimiteUnidades = table.Column<int>(type: "INTEGER", nullable: false),
                    PlnLimiteProfissionais = table.Column<int>(type: "INTEGER", nullable: false),
                    PlnLimiteAgendamentosMes = table.Column<int>(type: "INTEGER", nullable: false),
                    PlnPublico = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlnAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlnOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    PlnCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plano", x => x.PlnId);
                });

            migrationBuilder.CreateTable(
                name: "Assinatura",
                columns: table => new
                {
                    AssId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_PlnId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    AssGateway = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AssGatewayPreapprovalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AssDataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssTrialAteEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssProximoVencimento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssUltimoPagamentoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssAtrasoDesde = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssReadOnlyDesde = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssCanceladaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssExpiradaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssPayloadGateway = table.Column<string>(type: "TEXT", nullable: true),
                    AssCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinatura", x => x.AssId);
                    table.ForeignKey(
                        name: "FK_Assinatura_Plano_R_PlnId",
                        column: x => x.R_PlnId,
                        principalTable: "Plano",
                        principalColumn: "PlnId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assinatura_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaturaAssinatura",
                columns: table => new
                {
                    FasId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_AssId = table.Column<int>(type: "INTEGER", nullable: false),
                    FasValor = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    FasStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    FasGatewayPaymentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FasReferenciaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FasReferenciaFim = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FasVencimentoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FasPagoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FasPayloadGateway = table.Column<string>(type: "TEXT", nullable: true),
                    FasCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaAssinatura", x => x.FasId);
                    table.ForeignKey(
                        name: "FK_FaturaAssinatura_Assinatura_R_AssId",
                        column: x => x.R_AssId,
                        principalTable: "Assinatura",
                        principalColumn: "AssId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Plano",
                columns: new[] { "PlnId", "PlnAtivo", "PlnCriadoEm", "PlnDescricao", "PlnLimiteAgendamentosMes", "PlnLimiteProfissionais", "PlnLimiteUnidades", "PlnNome", "PlnOrdem", "PlnPreco", "PlnPublico" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Ideal para 1 unidade. Tudo que você precisa para começar.", -1, 10, 1, "Essencial", 1, 29.90m, true },
                    { 2, true, new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Para redes e franquias. Unidades ilimitadas, profissionais ilimitados.", -1, -1, -1, "Multi-unidade", 2, 79.90m, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinatura_AssGatewayPreapprovalId",
                table: "Assinatura",
                column: "AssGatewayPreapprovalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assinatura_AssStatus",
                table: "Assinatura",
                column: "AssStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Assinatura_R_PlnId",
                table: "Assinatura",
                column: "R_PlnId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinatura_R_TenId",
                table: "Assinatura",
                column: "R_TenId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaAssinatura_FasGatewayPaymentId",
                table: "FaturaAssinatura",
                column: "FasGatewayPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaturaAssinatura_R_AssId",
                table: "FaturaAssinatura",
                column: "R_AssId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaAssinatura_R_TenId_FasReferenciaInicio",
                table: "FaturaAssinatura",
                columns: new[] { "R_TenId", "FasReferenciaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Plano_PlnAtivo_PlnPublico",
                table: "Plano",
                columns: new[] { "PlnAtivo", "PlnPublico" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaturaAssinatura");

            migrationBuilder.DropTable(
                name: "Assinatura");

            migrationBuilder.DropTable(
                name: "Plano");
        }
    }
}
