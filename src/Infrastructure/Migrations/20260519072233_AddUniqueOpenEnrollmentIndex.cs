using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOpenEnrollmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AcademicPeriods_IsEnrollmentOpen",
                table: "AcademicPeriods",
                column: "IsEnrollmentOpen",
                unique: true,
                filter: "[IsEnrollmentOpen] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcademicPeriods_IsEnrollmentOpen",
                table: "AcademicPeriods");
        }
    }
}
