using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGrupoComboIdAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgeGrupoComboId",
                table: "Agendamento",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_AgeGrupoComboId",
                table: "Agendamento",
                column: "AgeGrupoComboId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamento_AgeGrupoComboId",
                table: "Agendamento");

            migrationBuilder.DropColumn(
                name: "AgeGrupoComboId",
                table: "Agendamento");
        }
    }
}
