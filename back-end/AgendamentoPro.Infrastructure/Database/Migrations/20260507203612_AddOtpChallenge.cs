using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtpChallenge",
                columns: table => new
                {
                    OtpId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    R_TenId = table.Column<int>(type: "INTEGER", nullable: false),
                    OtpTelefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OtpCodigoHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OtpCriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OtpExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OtpTentativas = table.Column<int>(type: "INTEGER", nullable: false),
                    OtpUsado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenge", x => x.OtpId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenge_R_TenId_OtpTelefone_OtpCriadoEm",
                table: "OtpChallenge",
                columns: new[] { "R_TenId", "OtpTelefone", "OtpCriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpChallenge");
        }
    }
}
