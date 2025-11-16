using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceReportingManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicantName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicantEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DepartmentGroup = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SeniorResponsibleOfficer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HasPortfolioSupport = table.Column<bool>(type: "bit", nullable: false),
                    PortfolioName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PortfolioPrioritisation = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProposedTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OverviewAndBusinessNeed = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    PreviousResearchOrInsight = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    WillCreateOrChangeDigitalService = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsManifestoOrStatutory = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OpportunityMissionPillar = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DdatStrategicThemes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    RiskIfNotDelivered = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    HasFunding = table.Column<bool>(type: "bit", nullable: false),
                    FundingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FundingSource = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingDuration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    HasHeadcount = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfFTE = table.Column<int>(type: "int", nullable: true),
                    RolesProvided = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HeadcountDuration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HeadcountNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    HasTargetDeliveryDate = table.Column<bool>(type: "bit", nullable: false),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignedToName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CurrentPhase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeclarationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReportingBusinessAreaConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessAreaName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicableFromYear = table.Column<int>(type: "int", nullable: false),
                    ApplicableFromMonth = table.Column<int>(type: "int", nullable: false),
                    ApplicableUntilYear = table.Column<int>(type: "int", nullable: true),
                    ApplicableUntilMonth = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReportingBusinessAreaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReportingDueDateOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingYear = table.Column<int>(type: "int", nullable: false),
                    ReportingMonth = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReportingDueDateOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReportingProductExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExclusionReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExclusionFromYear = table.Column<int>(type: "int", nullable: false),
                    ExclusionFromMonth = table.Column<int>(type: "int", nullable: false),
                    ExclusionUntilYear = table.Column<int>(type: "int", nullable: true),
                    ExclusionUntilMonth = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReportingProductExclusions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestContacts_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestPrioritisations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    StatutoryManifestoScore = table.Column<int>(type: "int", nullable: false),
                    OpportunityMissionScore = table.Column<int>(type: "int", nullable: false),
                    DdatStrategicThemeScore = table.Column<int>(type: "int", nullable: false),
                    ScaleOfUsersScore = table.Column<int>(type: "int", nullable: false),
                    EvidenceOfUserNeedScore = table.Column<int>(type: "int", nullable: false),
                    RiskIfNotDeliveredScore = table.Column<int>(type: "int", nullable: false),
                    TargetDeliveryUrgencyScore = table.Column<int>(type: "int", nullable: false),
                    FundingAvailableScore = table.Column<int>(type: "int", nullable: false),
                    HeadcountAvailableScore = table.Column<int>(type: "int", nullable: false),
                    PortfolioFitScore = table.Column<int>(type: "int", nullable: false),
                    ExpectedBenefitsScore = table.Column<int>(type: "int", nullable: false),
                    StrategicAlignmentTotal = table.Column<int>(type: "int", nullable: false),
                    UserImpactTotal = table.Column<int>(type: "int", nullable: false),
                    RiskUrgencyTotal = table.Column<int>(type: "int", nullable: false),
                    FeasibilityTotal = table.Column<int>(type: "int", nullable: false),
                    ValueOutcomeTotal = table.Column<int>(type: "int", nullable: false),
                    TotalPriorityScore = table.Column<int>(type: "int", nullable: false),
                    PriorityTier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ScoringNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ScoredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestPrioritisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestPrioritisations_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestContacts_DemandRequestId",
                table: "DemandRequestContacts",
                column: "DemandRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestPrioritisations_DemandRequestId",
                table: "DemandRequestPrioritisations",
                column: "DemandRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestPrioritisations_PriorityTier",
                table: "DemandRequestPrioritisations",
                column: "PriorityTier");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestPrioritisations_TotalPriorityScore",
                table: "DemandRequestPrioritisations",
                column: "TotalPriorityScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_ApplicantEmail",
                table: "DemandRequests",
                column: "ApplicantEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_AssignedToEmail",
                table: "DemandRequests",
                column: "AssignedToEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_ReferenceNumber",
                table: "DemandRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_Status",
                table: "DemandRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_SubmittedAt",
                table: "DemandRequests",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingBusinessAreaConfigs_BusinessAreaName",
                table: "PerformanceReportingBusinessAreaConfigs",
                column: "BusinessAreaName");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingBusinessAreaConfigs_IsActive",
                table: "PerformanceReportingBusinessAreaConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingDueDateOverrides_IsActive",
                table: "PerformanceReportingDueDateOverrides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingDueDateOverrides_ReportingYear_ReportingMonth",
                table: "PerformanceReportingDueDateOverrides",
                columns: new[] { "ReportingYear", "ReportingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_FipsId",
                table: "PerformanceReportingProductExclusions",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_FipsId_IsActive",
                table: "PerformanceReportingProductExclusions",
                columns: new[] { "FipsId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingProductExclusions_IsActive",
                table: "PerformanceReportingProductExclusions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandRequestContacts");

            migrationBuilder.DropTable(
                name: "DemandRequestPrioritisations");

            migrationBuilder.DropTable(
                name: "PerformanceReportingBusinessAreaConfigs");

            migrationBuilder.DropTable(
                name: "PerformanceReportingDueDateOverrides");

            migrationBuilder.DropTable(
                name: "PerformanceReportingProductExclusions");

            migrationBuilder.DropTable(
                name: "DemandRequests");
        }
    }
}
