using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandTriageTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandRequestAssessments");

            migrationBuilder.DropTable(
                name: "DemandRequestContacts");

            migrationBuilder.DropTable(
                name: "DemandRequestNotes");

            migrationBuilder.DropTable(
                name: "DemandRequestPrioritisations");

            migrationBuilder.DropTable(
                name: "DemandRequestRiskTypes");

            migrationBuilder.DropTable(
                name: "DemandRequestSectionCompletions");

            migrationBuilder.DropTable(
                name: "DemandRequests");

            migrationBuilder.DropTable(
                name: "TriageMeetings");

            migrationBuilder.CreateTable(
                name: "DemandTriageRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestReference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequesterFullName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DepartmentGroup = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DdtPortfolioSupport = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PointsOfContact = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SroName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProposedRequestTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestOverview = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreviousResearch = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ManifestoOrStatute = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SosOpportunityMissionPillars = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DdtStrategicTheme = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RiskConsequence = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingProvided = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HeadcountProvided = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HeadcountDetails = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewOrChangedDigitalService = table.Column<bool>(type: "bit", nullable: true),
                    NewOrChangedServiceDetails = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PublicFacingDigitalService = table.Column<bool>(type: "bit", nullable: true),
                    OwnerUserEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerUserName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReturnedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: true),
                    ConvertedProjectId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandTriageRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandTriageRequests_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DemandTriageRequests_Projects_ConvertedProjectId",
                        column: x => x.ConvertedProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandExploratoryReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandTriageRequestId = table.Column<int>(type: "int", nullable: false),
                    SummaryFindings = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    KeyRisks = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RecommendationToProceed = table.Column<bool>(type: "bit", nullable: true),
                    ReasonNotProceeding = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandExploratoryReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandExploratoryReviews_DemandTriageRequests_DemandTriageRequestId",
                        column: x => x.DemandTriageRequestId,
                        principalTable: "DemandTriageRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandScorecards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandTriageRequestId = table.Column<int>(type: "int", nullable: false),
                    ScorecardStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    StrategicAlignmentScore = table.Column<int>(type: "int", nullable: false),
                    UrgencyScore = table.Column<int>(type: "int", nullable: false),
                    FundingScore = table.Column<int>(type: "int", nullable: false),
                    RiceScore = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    StrategicAlignmentBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UrgencyBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RiceBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SuggestionBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FinalisedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalisedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandScorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandScorecards_DemandTriageRequests_DemandTriageRequestId",
                        column: x => x.DemandTriageRequestId,
                        principalTable: "DemandTriageRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandTriageAuditEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandTriageRequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandTriageAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandTriageAuditEvents_DemandTriageRequests_DemandTriageRequestId",
                        column: x => x.DemandTriageRequestId,
                        principalTable: "DemandTriageRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandTriageOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandTriageRequestId = table.Column<int>(type: "int", nullable: false),
                    OutcomeSelection = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OutcomeSummary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RoutedToArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OverrodeRecommendation = table.Column<bool>(type: "bit", nullable: false),
                    OverrideReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandTriageOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandTriageOutcomes_DemandTriageRequests_DemandTriageRequestId",
                        column: x => x.DemandTriageRequestId,
                        principalTable: "DemandTriageRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandScorecardId = table.Column<int>(type: "int", nullable: false),
                    QuestionCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AnswerValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnswerScore = table.Column<int>(type: "int", nullable: true),
                    FreeText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandAnswers_DemandScorecards_DemandScorecardId",
                        column: x => x.DemandScorecardId,
                        principalTable: "DemandScorecards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandAnswers_DemandScorecardId_QuestionCode",
                table: "DemandAnswers",
                columns: new[] { "DemandScorecardId", "QuestionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandExploratoryReviews_DemandTriageRequestId",
                table: "DemandExploratoryReviews",
                column: "DemandTriageRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandScorecards_DemandTriageRequestId",
                table: "DemandScorecards",
                column: "DemandTriageRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageAuditEvents_DemandTriageRequestId",
                table: "DemandTriageAuditEvents",
                column: "DemandTriageRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageAuditEvents_OccurredAt",
                table: "DemandTriageAuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageOutcomes_DemandTriageRequestId",
                table: "DemandTriageOutcomes",
                column: "DemandTriageRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_BusinessCaseId",
                table: "DemandTriageRequests",
                column: "BusinessCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_ConvertedProjectId",
                table: "DemandTriageRequests",
                column: "ConvertedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_DeletedAt",
                table: "DemandTriageRequests",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_OwnerUserEmail",
                table: "DemandTriageRequests",
                column: "OwnerUserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_RequestReference",
                table: "DemandTriageRequests",
                column: "RequestReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_Status",
                table: "DemandTriageRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTriageRequests_SubmittedAt",
                table: "DemandTriageRequests",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandAnswers");

            migrationBuilder.DropTable(
                name: "DemandExploratoryReviews");

            migrationBuilder.DropTable(
                name: "DemandTriageAuditEvents");

            migrationBuilder.DropTable(
                name: "DemandTriageOutcomes");

            migrationBuilder.DropTable(
                name: "DemandScorecards");

            migrationBuilder.DropTable(
                name: "DemandTriageRequests");

            migrationBuilder.CreateTable(
                name: "TriageMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChairEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChairName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChairObjectId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageMeetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: true),
                    ConvertedProjectId = table.Column<int>(type: "int", nullable: true),
                    TriageMeetingId = table.Column<int>(type: "int", nullable: true),
                    ApplicantEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicantName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignedToName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConvertedToProjectAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPhase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DdatStrategicThemes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    DeclarationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    DeliveryTimescales = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    DigitalServiceDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    FundingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FundingDuration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    FundingSource = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HasFunding = table.Column<bool>(type: "bit", nullable: true),
                    HasHeadcount = table.Column<bool>(type: "bit", nullable: true),
                    HasPortfolioSupport = table.Column<bool>(type: "bit", nullable: true),
                    HasTargetDeliveryDate = table.Column<bool>(type: "bit", nullable: true),
                    HeadcountDuration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HeadcountNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ImpactLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ImpactSummary = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsManifestoOrStatutory = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsSensitiveRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsSubmittedToTriage = table.Column<bool>(type: "bit", nullable: true),
                    ManifestoStatutoryDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumberOfFTE = table.Column<int>(type: "int", nullable: true),
                    OpportunityMissionPillars = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OverviewAndBusinessNeed = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    PortfolioName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PortfolioPrioritisation = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PredictedRiskLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreviousResearchOrInsight = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ProposedTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RiskIfNotDelivered = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    RiskLevelOverride = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RolesProvided = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SeniorResponsibleOfficer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StatusChangeReason = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupportsDdatStrategicTheme = table.Column<bool>(type: "bit", nullable: true),
                    SupportsOpportunityMissionPillar = table.Column<bool>(type: "bit", nullable: true),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriageNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    TriageSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WillCreateOrChangeDigitalService = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequests_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DemandRequests_Projects_ConvertedProjectId",
                        column: x => x.ConvertedProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandRequests_TriageMeetings_TriageMeetingId",
                        column: x => x.TriageMeetingId,
                        principalTable: "TriageMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    AssessedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssessedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssessmentContent = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    AssessmentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestAssessments_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
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
                name: "DemandRequestNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NoteText = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestNotes_DemandRequests_DemandRequestId",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DdatStrategicThemeScore = table.Column<int>(type: "int", nullable: false),
                    EvidenceOfUserNeedScore = table.Column<int>(type: "int", nullable: false),
                    ExpectedBenefitsScore = table.Column<int>(type: "int", nullable: false),
                    FeasibilityTotal = table.Column<int>(type: "int", nullable: false),
                    FundingAvailableScore = table.Column<int>(type: "int", nullable: false),
                    HeadcountAvailableScore = table.Column<int>(type: "int", nullable: false),
                    OpportunityMissionScore = table.Column<int>(type: "int", nullable: false),
                    PortfolioFitScore = table.Column<int>(type: "int", nullable: false),
                    PriorityTier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RiskIfNotDeliveredScore = table.Column<int>(type: "int", nullable: false),
                    RiskUrgencyTotal = table.Column<int>(type: "int", nullable: false),
                    ScaleOfUsersScore = table.Column<int>(type: "int", nullable: false),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScoredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ScoringNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    StatutoryManifestoScore = table.Column<int>(type: "int", nullable: false),
                    StrategicAlignmentTotal = table.Column<int>(type: "int", nullable: false),
                    TargetDeliveryUrgencyScore = table.Column<int>(type: "int", nullable: false),
                    TotalPriorityScore = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserImpactTotal = table.Column<int>(type: "int", nullable: false),
                    ValueOutcomeTotal = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "DemandRequestRiskTypes",
                columns: table => new
                {
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    RiskTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestRiskTypes", x => new { x.DemandRequestId, x.RiskTypeId });
                    table.ForeignKey(
                        name: "FK_DemandRequestRiskTypes_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DemandRequestRiskTypes_RiskTypes_RiskTypeId",
                        column: x => x.RiskTypeId,
                        principalTable: "RiskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestSectionCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CompletionStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestSectionCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestSectionCompletions_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestAssessments_DemandRequestId_AssessmentType",
                table: "DemandRequestAssessments",
                columns: new[] { "DemandRequestId", "AssessmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestContacts_DemandRequestId",
                table: "DemandRequestContacts",
                column: "DemandRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestNotes_DemandRequestId",
                table: "DemandRequestNotes",
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
                name: "IX_DemandRequestRiskTypes_RiskTypeId",
                table: "DemandRequestRiskTypes",
                column: "RiskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_ApplicantEmail",
                table: "DemandRequests",
                column: "ApplicantEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_AssignedToEmail",
                table: "DemandRequests",
                column: "AssignedToEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_BusinessCaseId",
                table: "DemandRequests",
                column: "BusinessCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_ConvertedProjectId",
                table: "DemandRequests",
                column: "ConvertedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_IsSubmittedToTriage",
                table: "DemandRequests",
                column: "IsSubmittedToTriage");

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
                name: "IX_DemandRequests_TriageMeetingId",
                table: "DemandRequests",
                column: "TriageMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestSectionCompletions_DemandRequestId_SectionName",
                table: "DemandRequestSectionCompletions",
                columns: new[] { "DemandRequestId", "SectionName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_EndAt",
                table: "TriageMeetings",
                column: "EndAt");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_IsActive",
                table: "TriageMeetings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_StartAt",
                table: "TriageMeetings",
                column: "StartAt");
        }
    }
}
