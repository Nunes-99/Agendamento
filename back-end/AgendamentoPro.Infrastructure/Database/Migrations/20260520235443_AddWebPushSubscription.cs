using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWebPushSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebPushSubscription",
                columns: table => new
                {
                    PushId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    R_UsuId = table.Column<int>(type: "INTEGER", nullable: false),
                    PushEndpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PushP256dh = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PushAuth = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PushUserAgent = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PushCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PushUltimoEnvio = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebPushSubscription", x => x.PushId);
                    table.ForeignKey(
                        name: "FK_WebPushSubscription_Tenant_R_TenId",
                        column: x => x.R_TenId,
                        principalTable: "Tenant",
                        principalColumn: "TenId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebPushSubscription_Usuario_R_UsuId",
                        column: x => x.R_UsuId,
                        principalTable: "Usuario",
                        principalColumn: "UsuId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscription_PushEndpoint",
                table: "WebPushSubscription",
                column: "PushEndpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscription_R_TenId_R_UsuId",
                table: "WebPushSubscription",
                columns: new[] { "R_TenId", "R_UsuId" });

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscription_R_UsuId",
                table: "WebPushSubscription",
                column: "R_UsuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebPushSubscription");
        }
    }
}
