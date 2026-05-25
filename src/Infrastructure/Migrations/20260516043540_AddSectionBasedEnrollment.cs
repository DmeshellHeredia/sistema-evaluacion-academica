using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionBasedEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Users_ProfessorId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "StudentSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_ProfessorId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ProfessorId",
                table: "Subjects");

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "Grades",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SectionEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionEnrollments_SubjectSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "SubjectSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_SectionId",
                table: "Grades",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionEnrollments_SectionId",
                table: "SectionEnrollments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionEnrollments_StudentId_SectionId",
                table: "SectionEnrollments",
                columns: new[] { "StudentId", "SectionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_SubjectSections_SectionId",
                table: "Grades",
                column: "SectionId",
                principalTable: "SubjectSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_SubjectSections_SectionId",
                table: "Grades");

            migrationBuilder.DropTable(
                name: "SectionEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Grades_SectionId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Grades");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfessorId",
                table: "Subjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSubjects_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ProfessorId",
                table: "Subjects",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubjects_StudentId",
                table: "StudentSubjects",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubjects_SubjectId",
                table: "StudentSubjects",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Users_ProfessorId",
                table: "Subjects",
                column: "ProfessorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
