using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLogAuditoriaSoftDeleteFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogAuditoria",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: true),
                    R_UsuId = table.Column<int>(type: "INTEGER", nullable: true),
                    LogUsuarioEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    LogIp = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LogCorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LogTabela = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LogChave = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LogAcao = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LogValoresAntes = table.Column<string>(type: "TEXT", nullable: true),
                    LogValoresDepois = table.Column<string>(type: "TEXT", nullable: true),
                    LogQuandoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAuditoria", x => x.LogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogAuditoria_LogTabela_LogChave",
                table: "LogAuditoria",
                columns: new[] { "LogTabela", "LogChave" });

            migrationBuilder.CreateIndex(
                name: "IX_LogAuditoria_R_TenId_LogQuandoUtc",
                table: "LogAuditoria",
                columns: new[] { "R_TenId", "LogQuandoUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAuditoria");
        }
    }
}
