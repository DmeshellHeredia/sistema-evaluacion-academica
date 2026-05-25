using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSemesterLevelToSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SemesterLevel",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SemesterLevel",
                table: "Subjects");
        }
    }
}
