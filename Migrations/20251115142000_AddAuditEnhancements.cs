using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AfterJson",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeJson",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedByEmail",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedByUserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityReference",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql("UPDATE AuditLogs SET AfterJson = PayloadJson");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "AuditLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "AuditLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql("UPDATE AuditLogs SET PayloadJson = AfterJson");

            migrationBuilder.DropColumn(
                name: "AfterJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BeforeJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ChangedByEmail",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityReference",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");
        }
    }
}

