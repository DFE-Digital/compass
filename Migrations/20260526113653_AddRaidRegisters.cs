using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidRegisters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DirectorateLookupId = table.Column<int>(type: "int", nullable: true),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidRegisters_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisters_DirectorateLookups_DirectorateLookupId",
                        column: x => x.DirectorateLookupId,
                        principalTable: "DirectorateLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisters_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterAssumptions",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    AssumptionId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterAssumptions", x => new { x.RaidRegisterId, x.AssumptionId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterAssumptions_Assumptions_AssumptionId",
                        column: x => x.AssumptionId,
                        principalTable: "Assumptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterAssumptions_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterAssumptions_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterDependencies",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    DependencyId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterDependencies", x => new { x.RaidRegisterId, x.DependencyId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterDependencies_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterDependencies_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterDependencies_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterIssues",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterIssues", x => new { x.RaidRegisterId, x.IssueId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterIssues_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterIssues_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterIssues_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterNearMisses",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    NearMissId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterNearMisses", x => new { x.RaidRegisterId, x.NearMissId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterNearMisses_NearMisses_NearMissId",
                        column: x => x.NearMissId,
                        principalTable: "NearMisses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterNearMisses_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterNearMisses_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterRisks",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterRisks", x => new { x.RaidRegisterId, x.RiskId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterRisks_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterRisks_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterRisks_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterServices",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    FipsServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterServices", x => new { x.RaidRegisterId, x.FipsServiceId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterServices_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterServices_Service_FipsServiceId",
                        column: x => x.FipsServiceId,
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterUsers",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterUsers", x => new { x.RaidRegisterId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterUsers_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidRegisterUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaidRegisterWorkItems",
                columns: table => new
                {
                    RaidRegisterId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterWorkItems", x => new { x.RaidRegisterId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_RaidRegisterWorkItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidRegisterWorkItems_RaidRegisters_RaidRegisterId",
                        column: x => x.RaidRegisterId,
                        principalTable: "RaidRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterAssumptions_AddedByUserId",
                table: "RaidRegisterAssumptions",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterAssumptions_AssumptionId",
                table: "RaidRegisterAssumptions",
                column: "AssumptionId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterDependencies_AddedByUserId",
                table: "RaidRegisterDependencies",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterDependencies_DependencyId",
                table: "RaidRegisterDependencies",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterIssues_AddedByUserId",
                table: "RaidRegisterIssues",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterIssues_IssueId",
                table: "RaidRegisterIssues",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterNearMisses_AddedByUserId",
                table: "RaidRegisterNearMisses",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterNearMisses_NearMissId",
                table: "RaidRegisterNearMisses",
                column: "NearMissId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterRisks_AddedByUserId",
                table: "RaidRegisterRisks",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterRisks_RiskId",
                table: "RaidRegisterRisks",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisters_BusinessAreaLookupId",
                table: "RaidRegisters",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisters_CreatedByUserId",
                table: "RaidRegisters",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisters_DirectorateLookupId",
                table: "RaidRegisters",
                column: "DirectorateLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterServices_FipsServiceId",
                table: "RaidRegisterServices",
                column: "FipsServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterUsers_UserId",
                table: "RaidRegisterUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterWorkItems_ProjectId",
                table: "RaidRegisterWorkItems",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidRegisterAssumptions");

            migrationBuilder.DropTable(
                name: "RaidRegisterDependencies");

            migrationBuilder.DropTable(
                name: "RaidRegisterIssues");

            migrationBuilder.DropTable(
                name: "RaidRegisterNearMisses");

            migrationBuilder.DropTable(
                name: "RaidRegisterRisks");

            migrationBuilder.DropTable(
                name: "RaidRegisterServices");

            migrationBuilder.DropTable(
                name: "RaidRegisterUsers");

            migrationBuilder.DropTable(
                name: "RaidRegisterWorkItems");

            migrationBuilder.DropTable(
                name: "RaidRegisters");
        }
    }
}
