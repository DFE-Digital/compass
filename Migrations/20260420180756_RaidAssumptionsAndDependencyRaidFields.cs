using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidAssumptionsAndDependencyRaidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DependencyCriticalityId",
                table: "Dependencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependencyLinkTypeId",
                table: "Dependencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Dependencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Organisation",
                table: "Dependencies",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Dependencies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssumptionCriticalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssumptionCriticalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssumptionStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssumptionStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependencyCriticalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyCriticalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependencyLinkTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyLinkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    AssumptionCriticalityId = table.Column<int>(type: "int", nullable: true),
                    AssumptionStatusId = table.Column<int>(type: "int", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidationOutcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assumptions_AssumptionCriticalities_AssumptionCriticalityId",
                        column: x => x.AssumptionCriticalityId,
                        principalTable: "AssumptionCriticalities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assumptions_AssumptionStatuses_AssumptionStatusId",
                        column: x => x.AssumptionStatusId,
                        principalTable: "AssumptionStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assumptions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assumptions_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_DependencyCriticalityId",
                table: "Dependencies",
                column: "DependencyCriticalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_DependencyLinkTypeId",
                table: "Dependencies",
                column: "DependencyLinkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_OwnerUserId",
                table: "Dependencies",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_AssumptionCriticalityId",
                table: "Assumptions",
                column: "AssumptionCriticalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_AssumptionStatusId",
                table: "Assumptions",
                column: "AssumptionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_OwnerUserId",
                table: "Assumptions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_ProjectId_IsDeleted",
                table: "Assumptions",
                columns: new[] { "ProjectId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_Dependencies_DependencyCriticalities_DependencyCriticalityId",
                table: "Dependencies",
                column: "DependencyCriticalityId",
                principalTable: "DependencyCriticalities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dependencies_DependencyLinkTypes_DependencyLinkTypeId",
                table: "Dependencies",
                column: "DependencyLinkTypeId",
                principalTable: "DependencyLinkTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dependencies_Users_OwnerUserId",
                table: "Dependencies",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dependencies_DependencyCriticalities_DependencyCriticalityId",
                table: "Dependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependencies_DependencyLinkTypes_DependencyLinkTypeId",
                table: "Dependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependencies_Users_OwnerUserId",
                table: "Dependencies");

            migrationBuilder.DropTable(
                name: "Assumptions");

            migrationBuilder.DropTable(
                name: "DependencyCriticalities");

            migrationBuilder.DropTable(
                name: "DependencyLinkTypes");

            migrationBuilder.DropTable(
                name: "AssumptionCriticalities");

            migrationBuilder.DropTable(
                name: "AssumptionStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Dependencies_DependencyCriticalityId",
                table: "Dependencies");

            migrationBuilder.DropIndex(
                name: "IX_Dependencies_DependencyLinkTypeId",
                table: "Dependencies");

            migrationBuilder.DropIndex(
                name: "IX_Dependencies_OwnerUserId",
                table: "Dependencies");

            migrationBuilder.DropColumn(
                name: "DependencyCriticalityId",
                table: "Dependencies");

            migrationBuilder.DropColumn(
                name: "DependencyLinkTypeId",
                table: "Dependencies");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Dependencies");

            migrationBuilder.DropColumn(
                name: "Organisation",
                table: "Dependencies");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Dependencies");
        }
    }
}
