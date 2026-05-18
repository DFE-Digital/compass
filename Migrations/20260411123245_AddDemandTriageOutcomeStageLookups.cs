using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandTriageOutcomeStageLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriagePrimaryContactUserId",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriageStageLookupId",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DemandTriageOutcomeStages",
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
                    table.PrimaryKey("PK_DemandTriageOutcomeStages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests",
                column: "TriageAssignedBusinessAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_TriagePrimaryContactUserId",
                table: "DemandPipelineRequests",
                column: "TriagePrimaryContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_TriageStageLookupId",
                table: "DemandPipelineRequests",
                column: "TriageStageLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandPipelineRequests_BusinessAreaLookups_TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests",
                column: "TriageAssignedBusinessAreaId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandPipelineRequests_DemandTriageOutcomeStages_TriageStageLookupId",
                table: "DemandPipelineRequests",
                column: "TriageStageLookupId",
                principalTable: "DemandTriageOutcomeStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandPipelineRequests_Users_TriagePrimaryContactUserId",
                table: "DemandPipelineRequests",
                column: "TriagePrimaryContactUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                INSERT INTO [DemandTriageOutcomeStages] ([Code], [Label], [Description], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                (N'ACTIVE', N'Active', NULL, 10, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                (N'DRAFT', N'Draft', NULL, 20, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                (N'IN_DELIVERY', N'In delivery', NULL, 30, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                (N'REJECTED', N'Rejected', NULL, 40, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                (N'PAUSED', N'Paused', NULL, 50, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandPipelineRequests_BusinessAreaLookups_TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandPipelineRequests_DemandTriageOutcomeStages_TriageStageLookupId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandPipelineRequests_Users_TriagePrimaryContactUserId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropTable(
                name: "DemandTriageOutcomeStages");

            migrationBuilder.DropIndex(
                name: "IX_DemandPipelineRequests_TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandPipelineRequests_TriagePrimaryContactUserId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandPipelineRequests_TriageStageLookupId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriageAssignedBusinessAreaId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriagePrimaryContactUserId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriageStageLookupId",
                table: "DemandPipelineRequests");
        }
    }
}
