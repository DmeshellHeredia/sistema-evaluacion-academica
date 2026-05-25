using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaEvaluacionAcademica.Infrastructure.Data;

#nullable disable

namespace SistemaEvaluacionAcademica.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520200000_AddSecurityStampToUsers")]
    /// <inheritdoc />
    public partial class AddSecurityStampToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");
        }
    }
}
