using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    TenId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenSlug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TenSegmento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TenCnpj = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    TenEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TenTelefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    TenWhatsApp = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    TenEndereco = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TenCidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TenEstado = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    TenCep = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    TenLogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenBannerUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenFaviconUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenCorPrimaria = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TenCorSecundaria = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TenCorAcento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TenFonte = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TenDescricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TenPercentualEntrada = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    TenBufferMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    TenAntecedenciaMinHoras = table.Column<int>(type: "INTEGER", nullable: false),
                    TenAntecedenciaMaxDias = table.Column<int>(type: "INTEGER", nullable: false),
                    TenLimiteCancelamentoHoras = table.Column<int>(type: "INTEGER", nullable: false),
                    TenAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.TenId);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvento",
                columns: table => new
                {
                    WhEvId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WhEvGateway = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    WhEvEventoId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WhEvTipo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WhEvRecebidoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WhEvProcessadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WhEvPayload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvento", x => x.WhEvId);
                });

            migrationBuilder.CreateTable(
                name: "BloqueioAgenda",
                columns: table => new
                {
                    BloId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_RecId = table.Column<int>(type: "INTEGER", nullable: true),
                    BloDataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BloDataFim = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BloMotivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BloCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueioAgenda", x => x.BloId);
                    table.ForeignKey(
                        name: "FK_BloqueioAgenda_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    CliId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    CliNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CliEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CliTelefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CliWhatsApp = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CliCpf = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    CliObservacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CliCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.CliId);
                    table.ForeignKey(
                        name: "FK_Cliente_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracaoTenant",
                columns: table => new
                {
                    CfgId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    CfgChave = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CfgValor = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CfgGrupo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CfgSensivel = table.Column<bool>(type: "INTEGER", nullable: false),
                    CfgAtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracaoTenant", x => x.CfgId);
                    table.ForeignKey(
                        name: "FK_ConfiguracaoTenant_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorarioFuncionamento",
                columns: table => new
                {
                    HorId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    HorDiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    HorAbertura = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HorFechamento = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HorPausaInicio = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    HorPausaFim = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    HorAberto = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioFuncionamento", x => x.HorId);
                    table.ForeignKey(
                        name: "FK_HorarioFuncionamento_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificacao",
                columns: table => new
                {
                    NotId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_AgeId = table.Column<int>(type: "INTEGER", nullable: true),
                    NotCanal = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NotTipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NotDestinatario = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NotMensagem = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    NotStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NotErro = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NotCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotEnviadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.NotId);
                    table.ForeignKey(
                        name: "FK_Notificacao_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recurso",
                columns: table => new
                {
                    RecId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecNome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    RecDescricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecTipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RecImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    RecCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recurso", x => x.RecId);
                    table.ForeignKey(
                        name: "FK_Recurso_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Servico",
                columns: table => new
                {
                    SerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    SerNome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SerDescricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SerPreco = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    SerDuracaoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    SerImagemUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SerCategoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SerAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    SerOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    SerCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servico", x => x.SerId);
                    table.ForeignKey(
                        name: "FK_Servico_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UsuEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UsuSenha = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    UsuPerfil = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UsuTelefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    UsuAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsuUltimoLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuId);
                    table.ForeignKey(
                        name: "FK_Usuario_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId");
                });

            migrationBuilder.CreateTable(
                name: "Agendamento",
                columns: table => new
                {
                    AgeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_CliId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_SerId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_RecId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgeData = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgeHoraInicio = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    AgeHoraFim = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    AgeStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    AgePagamentoStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    AgeValorTotal = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    AgeValorEntrada = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    AgePercentualEntrada = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    AgeObservacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AgeMotivoCancelamento = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AgeCanceladoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AgeCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgeAtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamento", x => x.AgeId);
                    table.ForeignKey(
                        name: "FK_Agendamento_Cliente_R_CliId",
                        column: x => x.R_CliId,
                        principalTable: "Cliente",
                        principalColumn: "CliId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agendamento_Recurso_R_RecId",
                        column: x => x.R_RecId,
                        principalTable: "Recurso",
                        principalColumn: "RecId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agendamento_Servico_R_SerId",
                        column: x => x.R_SerId,
                        principalTable: "Servico",
                        principalColumn: "SerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agendamento_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    RefId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_UsuId = table.Column<int>(type: "INTEGER", nullable: false),
                    RefToken = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RefJwtId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RefUtilizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefRevogado = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefExpiracao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.RefId);
                    table.ForeignKey(
                        name: "FK_RefreshToken_Usuario_R_UsuId",
                        column: x => x.R_UsuId,
                        principalTable: "Usuario",
                        principalColumn: "UsuId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagamento",
                columns: table => new
                {
                    PagId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_AgeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PagForma = table.Column<int>(type: "INTEGER", nullable: false),
                    PagStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PagValor = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    PagGateway = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PagGatewayId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PagQrCode = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PagLinkPagamento = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PagExpiracao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PagAprovadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PagPayloadGateway = table.Column<string>(type: "TEXT", nullable: true),
                    PagCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamento", x => x.PagId);
                    table.ForeignKey(
                        name: "FK_Pagamento_Agendamento_R_AgeId",
                        column: x => x.R_AgeId,
                        principalTable: "Agendamento",
                        principalColumn: "AgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pagamento_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_CliId",
                table: "Agendamento",
                column: "R_CliId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_RecId_AgeData_AgeHoraInicio",
                table: "Agendamento",
                columns: new[] { "R_RecId", "AgeData", "AgeHoraInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_SerId",
                table: "Agendamento",
                column: "R_SerId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_TenId_AgeData",
                table: "Agendamento",
                columns: new[] { "R_TenId", "AgeData" });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueioAgenda_R_TenId",
                table: "BloqueioAgenda",
                column: "R_TenId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_R_TenId_CliEmail",
                table: "Cliente",
                columns: new[] { "R_TenId", "CliEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_R_TenId_CliTelefone",
                table: "Cliente",
                columns: new[] { "R_TenId", "CliTelefone" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoTenant_R_TenId_CfgChave",
                table: "ConfiguracaoTenant",
                columns: new[] { "R_TenId", "CfgChave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorarioFuncionamento_R_TenId_HorDiaSemana",
                table: "HorarioFuncionamento",
                columns: new[] { "R_TenId", "HorDiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_R_TenId",
                table: "Notificacao",
                column: "R_TenId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamento_PagGatewayId",
                table: "Pagamento",
                column: "PagGatewayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamento_R_AgeId",
                table: "Pagamento",
                column: "R_AgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagamento_R_TenId",
                table: "Pagamento",
                column: "R_TenId");

            migrationBuilder.CreateIndex(
                name: "IX_Recurso_R_TenId_RecAtivo",
                table: "Recurso",
                columns: new[] { "R_TenId", "RecAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_R_UsuId",
                table: "RefreshToken",
                column: "R_UsuId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_RefToken",
                table: "RefreshToken",
                column: "RefToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servico_R_TenId_SerAtivo",
                table: "Servico",
                columns: new[] { "R_TenId", "SerAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_TenSlug",
                table: "Tenant",
                column: "TenSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_R_TenId",
                table: "Usuario",
                column: "R_TenId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UsuEmail",
                table: "Usuario",
                column: "UsuEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvento_WhEvGateway_WhEvEventoId",
                table: "WebhookEvento",
                columns: new[] { "WhEvGateway", "WhEvEventoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueioAgenda");

            migrationBuilder.DropTable(
                name: "ConfiguracaoTenant");

            migrationBuilder.DropTable(
                name: "HorarioFuncionamento");

            migrationBuilder.DropTable(
                name: "Notificacao");

            migrationBuilder.DropTable(
                name: "Pagamento");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "WebhookEvento");

            migrationBuilder.DropTable(
                name: "Agendamento");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropTable(
                name: "Recurso");

            migrationBuilder.DropTable(
                name: "Servico");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
