using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessibilityServiceAdminFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatementVerificationMethod",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessibilityEmailConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityEmailConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessibilityRetestRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessibilityIssueId = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailSentToRequestor = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentToAdmin = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityRetestRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessibilityRetestRequests_AccessibilityIssues_AccessibilityIssueId",
                        column: x => x.AccessibilityIssueId,
                        principalTable: "AccessibilityIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityEmailConfigurations_IsActive",
                table: "AccessibilityEmailConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityEmailConfigurations_Purpose",
                table: "AccessibilityEmailConfigurations",
                column: "Purpose");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityEmailConfigurations_Purpose_EmailAddress",
                table: "AccessibilityEmailConfigurations",
                columns: new[] { "Purpose", "EmailAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityRetestRequests_AccessibilityIssueId",
                table: "AccessibilityRetestRequests",
                column: "AccessibilityIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityRetestRequests_IsCompleted",
                table: "AccessibilityRetestRequests",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityRetestRequests_RequestedAt",
                table: "AccessibilityRetestRequests",
                column: "RequestedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessibilityEmailConfigurations");

            migrationBuilder.DropTable(
                name: "AccessibilityRetestRequests");

            migrationBuilder.DropColumn(
                name: "StatementVerificationMethod",
                table: "ProductAccessibilities");
        }
    }
}
