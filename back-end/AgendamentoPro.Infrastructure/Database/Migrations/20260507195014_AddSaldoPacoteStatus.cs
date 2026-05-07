using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSaldoPacoteStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SaldGatewayPagamentoId",
                table: "SaldoPacote",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaldPagoEm",
                table: "SaldoPacote",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaldStatus",
                table: "SaldoPacote",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SaldoPacote_SaldGatewayPagamentoId",
                table: "SaldoPacote",
                column: "SaldGatewayPagamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SaldoPacote_SaldGatewayPagamentoId",
                table: "SaldoPacote");

            migrationBuilder.DropColumn(
                name: "SaldGatewayPagamentoId",
                table: "SaldoPacote");

            migrationBuilder.DropColumn(
                name: "SaldPagoEm",
                table: "SaldoPacote");

            migrationBuilder.DropColumn(
                name: "SaldStatus",
                table: "SaldoPacote");
        }
    }
}
