using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityTypeLookupId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlphaEndDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlphaEndDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlphaStartDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlphaStartDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscoveryEndDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscoveryEndDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscoveryStartDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscoveryStartDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivateBetaEndDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivateBetaEndDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivateBetaStartDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivateBetaStartDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicBetaEndDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicBetaEndDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicBetaStartDateActual",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicBetaStartDatePlanned",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskAppetiteLookupId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceUsers",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionDecisions",
                columns: table => new
                {
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionDecisions", x => new { x.ActionId, x.DecisionId });
                    table.ForeignKey(
                        name: "FK_ActionDecisions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionDecisions_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTypeLookups",
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
                    table.PrimaryKey("PK_ActivityTypeLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorateLookups",
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
                    table.PrimaryKey("PK_DirectorateLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueRisks",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    RiskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueRisks", x => new { x.IssueId, x.RiskId });
                    table.ForeignKey(
                        name: "FK_IssueRisks_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueRisks_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBudgetOwners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBudgetOwners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBudgetOwners_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectBudgetOwners_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPmoContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPmoContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPmoContacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPmoContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSeniorResponsibleOfficers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSeniorResponsibleOfficers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSeniorResponsibleOfficers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSeniorResponsibleOfficers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStatusUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatusUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectStatusUpdates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectStatusUpdates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectStatusUpdates_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RiskAppetiteLookups",
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
                    table.PrimaryKey("PK_RiskAppetiteLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DirectorateLookupId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDirectorates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                        column: x => x.DirectorateLookupId,
                        principalTable: "DirectorateLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDirectorates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ActivityTypeLookupId",
                table: "Projects",
                column: "ActivityTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RiskAppetiteLookupId",
                table: "Projects",
                column: "RiskAppetiteLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionDecisions_DecisionId",
                table: "ActionDecisions",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypeLookups_IsActive",
                table: "ActivityTypeLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypeLookups_Name",
                table: "ActivityTypeLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectorateLookups_IsActive",
                table: "DirectorateLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DirectorateLookups_Name",
                table: "DirectorateLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueRisks_RiskId",
                table: "IssueRisks",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBudgetOwners_BusinessAreaLookupId",
                table: "ProjectBudgetOwners",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBudgetOwners_ProjectId",
                table: "ProjectBudgetOwners",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBudgetOwners_ProjectId_BusinessAreaLookupId",
                table: "ProjectBudgetOwners",
                columns: new[] { "ProjectId", "BusinessAreaLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId",
                table: "ProjectDirectorates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates",
                columns: new[] { "ProjectId", "DirectorateLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPmoContacts_ProjectId",
                table: "ProjectPmoContacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPmoContacts_ProjectId_UserId",
                table: "ProjectPmoContacts",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPmoContacts_UserId",
                table: "ProjectPmoContacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSeniorResponsibleOfficers_ProjectId",
                table: "ProjectSeniorResponsibleOfficers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSeniorResponsibleOfficers_ProjectId_UserId",
                table: "ProjectSeniorResponsibleOfficers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSeniorResponsibleOfficers_UserId",
                table: "ProjectSeniorResponsibleOfficers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_CreatedAt",
                table: "ProjectStatusUpdates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_CreatedByUserId",
                table: "ProjectStatusUpdates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_ProjectId",
                table: "ProjectStatusUpdates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatusUpdates_UpdatedByUserId",
                table: "ProjectStatusUpdates",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAppetiteLookups_IsActive",
                table: "RiskAppetiteLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAppetiteLookups_Name",
                table: "RiskAppetiteLookups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ActivityTypeLookups_ActivityTypeLookupId",
                table: "Projects",
                column: "ActivityTypeLookupId",
                principalTable: "ActivityTypeLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_RiskAppetiteLookups_RiskAppetiteLookupId",
                table: "Projects",
                column: "RiskAppetiteLookupId",
                principalTable: "RiskAppetiteLookups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ActivityTypeLookups_ActivityTypeLookupId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_RiskAppetiteLookups_RiskAppetiteLookupId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ActionDecisions");

            migrationBuilder.DropTable(
                name: "ActivityTypeLookups");

            migrationBuilder.DropTable(
                name: "IssueRisks");

            migrationBuilder.DropTable(
                name: "ProjectBudgetOwners");

            migrationBuilder.DropTable(
                name: "ProjectDirectorates");

            migrationBuilder.DropTable(
                name: "ProjectPmoContacts");

            migrationBuilder.DropTable(
                name: "ProjectSeniorResponsibleOfficers");

            migrationBuilder.DropTable(
                name: "ProjectStatusUpdates");

            migrationBuilder.DropTable(
                name: "RiskAppetiteLookups");

            migrationBuilder.DropTable(
                name: "DirectorateLookups");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ActivityTypeLookupId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_RiskAppetiteLookupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ActivityTypeLookupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AlphaEndDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AlphaEndDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AlphaStartDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AlphaStartDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DiscoveryEndDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DiscoveryEndDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DiscoveryStartDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DiscoveryStartDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrivateBetaEndDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrivateBetaEndDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrivateBetaStartDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrivateBetaStartDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PublicBetaEndDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PublicBetaEndDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PublicBetaStartDateActual",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PublicBetaStartDatePlanned",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RiskAppetiteLookupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ServiceUsers",
                table: "Projects");
        }
    }
}
