using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessorsCareerAndSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Career",
                table: "Subjects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SubjectPrerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectPrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectPrerequisites_Subjects_PrerequisiteSubjectId",
                        column: x => x.PrerequisiteSubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectPrerequisites_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Modality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectSections_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectSections_Users_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectPrerequisites_PrerequisiteSubjectId",
                table: "SubjectPrerequisites",
                column: "PrerequisiteSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectPrerequisites_SubjectId_PrerequisiteSubjectId",
                table: "SubjectPrerequisites",
                columns: new[] { "SubjectId", "PrerequisiteSubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSections_ProfessorId",
                table: "SubjectSections",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSections_SubjectId_SectionCode",
                table: "SubjectSections",
                columns: new[] { "SubjectId", "SectionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectPrerequisites");

            migrationBuilder.DropTable(
                name: "SubjectSections");

            migrationBuilder.DropColumn(
                name: "Career",
                table: "Subjects");
        }
    }
}
