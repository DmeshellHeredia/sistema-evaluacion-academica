using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomToSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classroom",
                table: "SubjectSections",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classroom",
                table: "SubjectSections");
        }
    }
}
