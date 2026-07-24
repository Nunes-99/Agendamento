using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendamentoPro.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class IndiceAgendamentoIgnoraCancelados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamento_R_RecId_AgeData_AgeHoraInicio",
                table: "Agendamento");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_RecId_AgeData_AgeHoraInicio",
                table: "Agendamento",
                columns: new[] { "R_RecId", "AgeData", "AgeHoraInicio" },
                unique: true,
                filter: "\"AgeStatus\" <> 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamento_R_RecId_AgeData_AgeHoraInicio",
                table: "Agendamento");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamento_R_RecId_AgeData_AgeHoraInicio",
                table: "Agendamento",
                columns: new[] { "R_RecId", "AgeData", "AgeHoraInicio" },
                unique: true);
        }
    }
}
