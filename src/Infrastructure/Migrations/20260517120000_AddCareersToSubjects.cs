using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCareersToSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Careers",
                table: "Subjects",
                type: "nvarchar(2000)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(@"
                UPDATE Subjects
                SET Careers = '[]'
                WHERE AppliesToAllCareers = 1 OR Career = ''");

            migrationBuilder.Sql(@"
                UPDATE Subjects
                SET Careers = CONCAT('[""', Career, '""]')
                WHERE AppliesToAllCareers = 0 AND Career != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Careers",
                table: "Subjects");
        }
    }
}
