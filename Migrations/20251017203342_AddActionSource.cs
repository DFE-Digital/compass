using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddActionSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionSourceId",
                table: "Actions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionSourceId",
                table: "Actions",
                column: "ActionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_Code",
                table: "ActionSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_IsActive",
                table: "ActionSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_SortOrder",
                table: "ActionSources",
                column: "SortOrder");

            // Seed Action Sources
            var now = DateTime.UtcNow;
            
            migrationBuilder.InsertData(
                table: "ActionSources",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "RISK", "Risk", "Action created to mitigate, treat, or manage a risk. These actions are typically focused on reducing impact or likelihood of risk materialisation.", "Action arising from a risk", 1, true, now, now });

            migrationBuilder.InsertData(
                table: "ActionSources",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "ISSUE", "Issue", "Action created to resolve, workaround, or close an issue. These actions address problems that have already materialised and require remediation.", "Action arising from an issue", 2, true, now, now });

            migrationBuilder.InsertData(
                table: "ActionSources",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "MILESTONE", "Milestone", "Action created to deliver a milestone or track progress towards milestone completion. These actions are typically project or programme delivery activities.", "Action arising from a milestone", 3, true, now, now });

            migrationBuilder.InsertData(
                table: "ActionSources",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "SERVICE_ASSESSMENT", "Service Assessment", "Action created as a result of a service assessment review. These actions may be created by external assessment teams or third-party reviewers and typically address recommendations or compliance requirements.", "Action from service assessment", 4, true, now, now });

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions",
                column: "ActionSourceId",
                principalTable: "ActionSources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions");

            migrationBuilder.DropTable(
                name: "ActionSources");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ActionSourceId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ActionSourceId",
                table: "Actions");
        }
    }
}
