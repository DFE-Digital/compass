using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandPipelineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandPipelineBusinessCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Lead = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LeadUserId = table.Column<int>(type: "int", nullable: true),
                    Sro = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SroUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DepartmentGroup = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DirectorateId = table.Column<int>(type: "int", nullable: true),
                    PortfolioId = table.Column<int>(type: "int", nullable: true),
                    GovernmentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    ProblemStatement = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    ProposedSolution = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    Benefits = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    StatutoryDriver = table.Column<bool>(type: "bit", nullable: true),
                    StatutoryDriverComments = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    StatutoryReference = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    FundingPosition = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FundingComments = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    LinkedWorkAndDemands = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    HeadcountIdentified = table.Column<bool>(type: "bit", nullable: true),
                    PriorityOutcomeId = table.Column<int>(type: "int", nullable: true),
                    MissionPillarId = table.Column<int>(type: "int", nullable: true),
                    TargetSubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedDemandRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandPipelineBusinessCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandPipelineRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BusinessCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    DemandTypeId = table.Column<int>(type: "int", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sro = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SroUserId = table.Column<int>(type: "int", nullable: true),
                    PointsOfContact = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    AssigneeUserId = table.Column<int>(type: "int", nullable: true),
                    Assignee = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DepartmentGroup = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DirectorateId = table.Column<int>(type: "int", nullable: true),
                    PortfolioId = table.Column<int>(type: "int", nullable: true),
                    GovernmentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    PreviousResearch = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    TargetDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManifestoCommitment = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    RiskIfNotDelivered = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    StatutoryDriver = table.Column<bool>(type: "bit", nullable: true),
                    PriorityOutcomeId = table.Column<int>(type: "int", nullable: true),
                    MissionPillarId = table.Column<int>(type: "int", nullable: true),
                    FundingProvided = table.Column<bool>(type: "bit", nullable: true),
                    HeadcountProvided = table.Column<bool>(type: "bit", nullable: true),
                    IsNewDigitalService = table.Column<bool>(type: "bit", nullable: true),
                    IsPublicFacing = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: true),
                    SuggestedBand = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ScoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScoredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TriageOutcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TriagedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriagedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandPipelineRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandPipelineStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Grouping = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandPipelineStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandPipelineTriageMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EndTime = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MeetingReference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Chair = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChairUserId = table.Column<int>(type: "int", nullable: true),
                    Attendees = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    CopilotSummaryNotes = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    AgendaJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandPipelineTriageMeetings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineBusinessCases_Reference",
                table: "DemandPipelineBusinessCases",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_BusinessCaseId",
                table: "DemandPipelineRequests",
                column: "BusinessCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_Reference",
                table: "DemandPipelineRequests",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_Status",
                table: "DemandPipelineRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineStages_DisplayOrder",
                table: "DemandPipelineStages",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineTriageMeetings_MeetingDate",
                table: "DemandPipelineTriageMeetings",
                column: "MeetingDate");

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineTriageMeetings_Status",
                table: "DemandPipelineTriageMeetings",
                column: "Status");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [DemandPipelineStages])
BEGIN
  INSERT INTO [DemandPipelineStages] ([Title], [DisplayOrder], [Description], [IsActive], [Grouping]) VALUES
  (N'Idea', 10, NULL, 1, N'Business case'),
  (N'Developing', 20, NULL, 1, N'Business case'),
  (N'Ready to submit', 30, NULL, 1, N'Business case'),
  (N'Demand register', 40, NULL, 1, N'Demand'),
  (N'Explore', 50, NULL, 1, N'Demand'),
  (N'Scoring', 60, NULL, 1, N'Demand'),
  (N'Triage', 70, NULL, 1, N'Demand'),
  (N'Discovery', 80, NULL, 1, N'Work'),
  (N'Live', 90, NULL, 1, N'Work');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandPipelineBusinessCases");

            migrationBuilder.DropTable(
                name: "DemandPipelineRequests");

            migrationBuilder.DropTable(
                name: "DemandPipelineStages");

            migrationBuilder.DropTable(
                name: "DemandPipelineTriageMeetings");
        }
    }
}
