using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLMSEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ResourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_SubjectSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "SubjectSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_SubjectSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "SubjectSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Announcements_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivitySubmissions_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivitySubmissions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SectionId",
                table: "Activities",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySubmissions_ActivityId_StudentId",
                table: "ActivitySubmissions",
                columns: new[] { "ActivityId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySubmissions_StudentId",
                table: "ActivitySubmissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_AuthorId",
                table: "Announcements",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_SectionId",
                table: "Announcements",
                column: "SectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivitySubmissions");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Activities");
        }
    }
}
