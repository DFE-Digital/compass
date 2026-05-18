using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class WorkItemCustomTagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItemTagLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemTagLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectWorkItemTags",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WorkItemTagLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWorkItemTags", x => new { x.ProjectId, x.WorkItemTagLookupId });
                    table.ForeignKey(
                        name: "FK_ProjectWorkItemTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectWorkItemTags_WorkItemTagLookups_WorkItemTagLookupId",
                        column: x => x.WorkItemTagLookupId,
                        principalTable: "WorkItemTagLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkItemTags_ProjectId",
                table: "ProjectWorkItemTags",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkItemTags_WorkItemTagLookupId",
                table: "ProjectWorkItemTags",
                column: "WorkItemTagLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemTagLookups_IsActive",
                table: "WorkItemTagLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemTagLookups_Name",
                table: "WorkItemTagLookups",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectWorkItemTags");

            migrationBuilder.DropTable(
                name: "WorkItemTagLookups");
        }
    }
}
