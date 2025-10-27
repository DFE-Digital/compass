using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalStructureAndGovernmentDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMultiDepartmentProject",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherDepartments",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryOrganizationalGroupId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GovernmentDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Format = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GovukStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WebUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnalyticsIdentifier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ParentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    GovukId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernmentDepartments_GovernmentDepartments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "GovernmentDepartments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ParentGroupId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationalGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationalGroups_OrganizationalGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "OrganizationalGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationalGroupId = table.Column<int>(type: "int", nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationalRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationalRoles_OrganizationalGroups_OrganizationalGroupId",
                        column: x => x.OrganizationalGroupId,
                        principalTable: "OrganizationalGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PrimaryOrganizationalGroupId",
                table: "Projects",
                column: "PrimaryOrganizationalGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentDepartments_ParentDepartmentId",
                table: "GovernmentDepartments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalGroups_ParentGroupId",
                table: "OrganizationalGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalRoles_OrganizationalGroupId",
                table: "OrganizationalRoles",
                column: "OrganizationalGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_OrganizationalGroups_PrimaryOrganizationalGroupId",
                table: "Projects",
                column: "PrimaryOrganizationalGroupId",
                principalTable: "OrganizationalGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_OrganizationalGroups_PrimaryOrganizationalGroupId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "GovernmentDepartments");

            migrationBuilder.DropTable(
                name: "OrganizationalRoles");

            migrationBuilder.DropTable(
                name: "OrganizationalGroups");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PrimaryOrganizationalGroupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsMultiDepartmentProject",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OtherDepartments",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrimaryOrganizationalGroupId",
                table: "Projects");
        }
    }
}
