using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDayOfWeekEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE SubjectSections SET DayOfWeek = 'Miercoles' WHERE DayOfWeek = N'Miércoles'");
            migrationBuilder.Sql("UPDATE SubjectSections SET DayOfWeek = 'Sabado'    WHERE DayOfWeek = N'Sábado'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE SubjectSections SET DayOfWeek = N'Miércoles' WHERE DayOfWeek = 'Miercoles'");
            migrationBuilder.Sql("UPDATE SubjectSections SET DayOfWeek = N'Sábado'    WHERE DayOfWeek = 'Sabado'");
        }
    }
}
