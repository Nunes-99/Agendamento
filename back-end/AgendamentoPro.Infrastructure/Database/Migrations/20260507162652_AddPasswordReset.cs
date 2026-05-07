using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordReset",
                columns: table => new
                {
                    RpsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_UsuId = table.Column<int>(type: "INTEGER", nullable: false),
                    RpsToken = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RpsExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RpsUsado = table.Column<bool>(type: "INTEGER", nullable: false),
                    RpsCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RpsUsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordReset", x => x.RpsId);
                    table.ForeignKey(
                        name: "FK_PasswordReset_Usuario_R_UsuId",
                        column: x => x.R_UsuId,
                        principalTable: "Usuario",
                        principalColumn: "UsuId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordReset_R_UsuId",
                table: "PasswordReset",
                column: "R_UsuId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordReset_RpsToken",
                table: "PasswordReset",
                column: "RpsToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordReset");
        }
    }
}
