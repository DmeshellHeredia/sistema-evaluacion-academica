using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubjectSections_SubjectId_IsActive",
                table: "SubjectSections",
                columns: new[] { "SubjectId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SectionEnrollments_StudentId_IsActive",
                table: "SectionEnrollments",
                columns: new[] { "StudentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubjectSections_SubjectId_IsActive",
                table: "SubjectSections");

            migrationBuilder.DropIndex(
                name: "IX_SectionEnrollments_StudentId_IsActive",
                table: "SectionEnrollments");
        }
    }
}
