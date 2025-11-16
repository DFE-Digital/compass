using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidCentralisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SourceType",
                table: "Risks",
                newName: "SourceId");

            migrationBuilder.RenameColumn(
                name: "SourceReference",
                table: "Risks",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "SourceRecordUrl",
                table: "Risks",
                newName: "ResponseStrategy");

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GovernanceBoardId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IdentifiedDate",
                table: "Risks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InherentScore",
                table: "Risks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewDate",
                table: "Risks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "Risks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryProductId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResidualScore",
                table: "Risks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskCategoryId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskImpactLevelId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskLikelihoodId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskPriorityId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskProximityId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskStatusId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "IssueWcagCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BlocksRelease",
                table: "Issues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueCategoryId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MilestoneId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryProductId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedDate",
                table: "Issues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceImpactSummary",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeverityId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserImpactSummary",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "IssueHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "IssueComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDocuments",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgendaItemReference",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecisionMakerUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "Decisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceTypeId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresDate",
                table: "Decisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GovernanceBoardId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImplementationDeadline",
                table: "Decisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImplementationOwnerUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImplementationStatusId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MilestoneId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutcomeId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryProductId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedByUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleTeam",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SponsorUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "Decisions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VerificationRequired",
                table: "Decisions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedByUserId",
                table: "Decisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDate",
                table: "Decisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountablePersonUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionTypeId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Blocked",
                table: "Actions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BlockedReason",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "Actions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalationThresholdId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EscalationTriggered",
                table: "Actions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceTypeId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpactLevelId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProgressUpdate",
                table: "Actions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecyclePhase",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MilestoneId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryProductId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "Actions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rag",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderFrequencyId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamName",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VerificationRequired",
                table: "Actions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedByUserId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDate",
                table: "Actions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionCategories",
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
                    table.PrimaryKey("PK_ActionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionEscalationThresholds",
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
                    table.PrimaryKey("PK_ActionEscalationThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionImpactLevels",
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
                    table.PrimaryKey("PK_ActionImpactLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionPriorities",
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
                    table.PrimaryKey("PK_ActionPriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionReminderFrequencies",
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
                    table.PrimaryKey("PK_ActionReminderFrequencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionStatuses",
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
                    table.PrimaryKey("PK_ActionStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionTags_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionTypes",
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
                    table.PrimaryKey("PK_ActionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionImplementationStatuses",
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
                    table.PrimaryKey("PK_DecisionImplementationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionOutcomes",
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
                    table.PrimaryKey("PK_DecisionOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionPriorities",
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
                    table.PrimaryKey("PK_DecisionPriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionStatuses",
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
                    table.PrimaryKey("PK_DecisionStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionTags_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceBoards",
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
                    table.PrimaryKey("PK_GovernanceBoards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueCategories",
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
                    table.PrimaryKey("PK_IssueCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssuePriorities",
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
                    table.PrimaryKey("PK_IssuePriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueSeverities",
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
                    table.PrimaryKey("PK_IssueSeverities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueStatuses",
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
                    table.PrimaryKey("PK_IssueStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueTags_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidEvidenceTypes",
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
                    table.PrimaryKey("PK_RaidEvidenceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskCategories",
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
                    table.PrimaryKey("PK_RiskCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskImpactLevels",
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
                    table.PrimaryKey("PK_RiskImpactLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskLikelihoods",
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
                    table.PrimaryKey("PK_RiskLikelihoods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskPriorities",
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
                    table.PrimaryKey("PK_RiskPriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskProximities",
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
                    table.PrimaryKey("PK_RiskProximities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskStatuses",
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
                    table.PrimaryKey("PK_RiskStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskTags_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ClosedByUserId",
                table: "Risks",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_CreatedByUserId",
                table: "Risks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_GovernanceBoardId",
                table: "Risks",
                column: "GovernanceBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_OwnerUserId",
                table: "Risks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_PrimaryProductId",
                table: "Risks",
                column: "PrimaryProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskCategoryId",
                table: "Risks",
                column: "RiskCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskImpactLevelId",
                table: "Risks",
                column: "RiskImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskLikelihoodId",
                table: "Risks",
                column: "RiskLikelihoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskPriorityId",
                table: "Risks",
                column: "RiskPriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskProximityId",
                table: "Risks",
                column: "RiskProximityId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskStatusId",
                table: "Risks",
                column: "RiskStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_UpdatedByUserId",
                table: "Risks",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueWcagCriteria_IssueId",
                table: "IssueWcagCriteria",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ClosedByUserId",
                table: "Issues",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_CreatedByUserId",
                table: "Issues",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueCategoryId",
                table: "Issues",
                column: "IssueCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_MilestoneId",
                table: "Issues",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PrimaryProductId",
                table: "Issues",
                column: "PrimaryProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PriorityId",
                table: "Issues",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RiskId",
                table: "Issues",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_SeverityId",
                table: "Issues",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_StatusId",
                table: "Issues",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_UpdatedByUserId",
                table: "Issues",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueHistories_IssueId",
                table: "IssueHistories",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueComments_IssueId",
                table: "IssueComments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ClosedByUserId",
                table: "Decisions",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CreatedByUserId",
                table: "Decisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_DecisionMakerUserId",
                table: "Decisions",
                column: "DecisionMakerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_EvidenceTypeId",
                table: "Decisions",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_GovernanceBoardId",
                table: "Decisions",
                column: "GovernanceBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ImplementationOwnerUserId",
                table: "Decisions",
                column: "ImplementationOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ImplementationStatusId",
                table: "Decisions",
                column: "ImplementationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_MilestoneId",
                table: "Decisions",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_OutcomeId",
                table: "Decisions",
                column: "OutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_PrimaryProductId",
                table: "Decisions",
                column: "PrimaryProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_PriorityId",
                table: "Decisions",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ProposedByUserId",
                table: "Decisions",
                column: "ProposedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_SponsorUserId",
                table: "Decisions",
                column: "SponsorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_StatusId",
                table: "Decisions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_UpdatedByUserId",
                table: "Decisions",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_VerifiedByUserId",
                table: "Decisions",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AccountablePersonUserId",
                table: "Actions",
                column: "AccountablePersonUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionTypeId",
                table: "Actions",
                column: "ActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AssignedToUserId",
                table: "Actions",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_CategoryId",
                table: "Actions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ClosedByUserId",
                table: "Actions",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_CreatedByUserId",
                table: "Actions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_EscalationThresholdId",
                table: "Actions",
                column: "EscalationThresholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_EvidenceTypeId",
                table: "Actions",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ImpactLevelId",
                table: "Actions",
                column: "ImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_IssueId",
                table: "Actions",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_MilestoneId",
                table: "Actions",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_PrimaryProductId",
                table: "Actions",
                column: "PrimaryProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_PriorityId",
                table: "Actions",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ReminderFrequencyId",
                table: "Actions",
                column: "ReminderFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_RiskId",
                table: "Actions",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ServiceId",
                table: "Actions",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_StatusId",
                table: "Actions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_UpdatedByUserId",
                table: "Actions",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_VerifiedByUserId",
                table: "Actions",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionTags_ActionId",
                table: "ActionTags",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionTags_DecisionId",
                table: "DecisionTags",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTags_IssueId",
                table: "IssueTags",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTags_RiskId",
                table: "RiskTags",
                column: "RiskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionCategories_CategoryId",
                table: "Actions",
                column: "CategoryId",
                principalTable: "ActionCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionEscalationThresholds_EscalationThresholdId",
                table: "Actions",
                column: "EscalationThresholdId",
                principalTable: "ActionEscalationThresholds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionImpactLevels_ImpactLevelId",
                table: "Actions",
                column: "ImpactLevelId",
                principalTable: "ActionImpactLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionPriorities_PriorityId",
                table: "Actions",
                column: "PriorityId",
                principalTable: "ActionPriorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionReminderFrequencies_ReminderFrequencyId",
                table: "Actions",
                column: "ReminderFrequencyId",
                principalTable: "ActionReminderFrequencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionStatuses_StatusId",
                table: "Actions",
                column: "StatusId",
                principalTable: "ActionStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionTypes_ActionTypeId",
                table: "Actions",
                column: "ActionTypeId",
                principalTable: "ActionTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Issues_IssueId",
                table: "Actions",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Milestones_MilestoneId",
                table: "Actions",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_RaidEvidenceTypes_EvidenceTypeId",
                table: "Actions",
                column: "EvidenceTypeId",
                principalTable: "RaidEvidenceTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Risks_RiskId",
                table: "Actions",
                column: "RiskId",
                principalTable: "Risks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Service_PrimaryProductId",
                table: "Actions",
                column: "PrimaryProductId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Service_ServiceId",
                table: "Actions",
                column: "ServiceId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_AccountablePersonUserId",
                table: "Actions",
                column: "AccountablePersonUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_AssignedToUserId",
                table: "Actions",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_ClosedByUserId",
                table: "Actions",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_CreatedByUserId",
                table: "Actions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_UpdatedByUserId",
                table: "Actions",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_VerifiedByUserId",
                table: "Actions",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_DecisionImplementationStatuses_ImplementationStatusId",
                table: "Decisions",
                column: "ImplementationStatusId",
                principalTable: "DecisionImplementationStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_DecisionOutcomes_OutcomeId",
                table: "Decisions",
                column: "OutcomeId",
                principalTable: "DecisionOutcomes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_DecisionPriorities_PriorityId",
                table: "Decisions",
                column: "PriorityId",
                principalTable: "DecisionPriorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_DecisionStatuses_StatusId",
                table: "Decisions",
                column: "StatusId",
                principalTable: "DecisionStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_GovernanceBoards_GovernanceBoardId",
                table: "Decisions",
                column: "GovernanceBoardId",
                principalTable: "GovernanceBoards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Milestones_MilestoneId",
                table: "Decisions",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_RaidEvidenceTypes_EvidenceTypeId",
                table: "Decisions",
                column: "EvidenceTypeId",
                principalTable: "RaidEvidenceTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Service_PrimaryProductId",
                table: "Decisions",
                column: "PrimaryProductId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_ClosedByUserId",
                table: "Decisions",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_CreatedByUserId",
                table: "Decisions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_DecisionMakerUserId",
                table: "Decisions",
                column: "DecisionMakerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_ImplementationOwnerUserId",
                table: "Decisions",
                column: "ImplementationOwnerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_ProposedByUserId",
                table: "Decisions",
                column: "ProposedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_SponsorUserId",
                table: "Decisions",
                column: "SponsorUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_UpdatedByUserId",
                table: "Decisions",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Users_VerifiedByUserId",
                table: "Decisions",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueComments_Issues_IssueId",
                table: "IssueComments",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueHistories_Issues_IssueId",
                table: "IssueHistories",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssueCategories_IssueCategoryId",
                table: "Issues",
                column: "IssueCategoryId",
                principalTable: "IssueCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssuePriorities_PriorityId",
                table: "Issues",
                column: "PriorityId",
                principalTable: "IssuePriorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssueSeverities_SeverityId",
                table: "Issues",
                column: "SeverityId",
                principalTable: "IssueSeverities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues",
                column: "StatusId",
                principalTable: "IssueStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Milestones_MilestoneId",
                table: "Issues",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Risks_RiskId",
                table: "Issues",
                column: "RiskId",
                principalTable: "Risks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Service_PrimaryProductId",
                table: "Issues",
                column: "PrimaryProductId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_ClosedByUserId",
                table: "Issues",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_CreatedByUserId",
                table: "Issues",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_UpdatedByUserId",
                table: "Issues",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueWcagCriteria_Issues_IssueId",
                table: "IssueWcagCriteria",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_GovernanceBoards_GovernanceBoardId",
                table: "Risks",
                column: "GovernanceBoardId",
                principalTable: "GovernanceBoards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskCategories_RiskCategoryId",
                table: "Risks",
                column: "RiskCategoryId",
                principalTable: "RiskCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskImpactLevels_RiskImpactLevelId",
                table: "Risks",
                column: "RiskImpactLevelId",
                principalTable: "RiskImpactLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskLikelihoods_RiskLikelihoodId",
                table: "Risks",
                column: "RiskLikelihoodId",
                principalTable: "RiskLikelihoods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskPriorities_RiskPriorityId",
                table: "Risks",
                column: "RiskPriorityId",
                principalTable: "RiskPriorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskProximities_RiskProximityId",
                table: "Risks",
                column: "RiskProximityId",
                principalTable: "RiskProximities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskStatuses_RiskStatusId",
                table: "Risks",
                column: "RiskStatusId",
                principalTable: "RiskStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Service_PrimaryProductId",
                table: "Risks",
                column: "PrimaryProductId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_ClosedByUserId",
                table: "Risks",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_CreatedByUserId",
                table: "Risks",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_OwnerUserId",
                table: "Risks",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_UpdatedByUserId",
                table: "Risks",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionCategories_CategoryId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionEscalationThresholds_EscalationThresholdId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionImpactLevels_ImpactLevelId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionPriorities_PriorityId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionReminderFrequencies_ReminderFrequencyId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionStatuses_StatusId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionTypes_ActionTypeId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Issues_IssueId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Milestones_MilestoneId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_RaidEvidenceTypes_EvidenceTypeId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Risks_RiskId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Service_PrimaryProductId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Service_ServiceId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_AccountablePersonUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_AssignedToUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_ClosedByUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_CreatedByUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_UpdatedByUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_VerifiedByUserId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_DecisionImplementationStatuses_ImplementationStatusId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_DecisionOutcomes_OutcomeId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_DecisionPriorities_PriorityId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_DecisionStatuses_StatusId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_GovernanceBoards_GovernanceBoardId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Milestones_MilestoneId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_RaidEvidenceTypes_EvidenceTypeId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Service_PrimaryProductId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_ClosedByUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_CreatedByUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_DecisionMakerUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_ImplementationOwnerUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_ProposedByUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_SponsorUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_UpdatedByUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Users_VerifiedByUserId",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueComments_Issues_IssueId",
                table: "IssueComments");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueHistories_Issues_IssueId",
                table: "IssueHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssueCategories_IssueCategoryId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssuePriorities_PriorityId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssueSeverities_SeverityId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Milestones_MilestoneId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Risks_RiskId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Service_PrimaryProductId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_ClosedByUserId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_CreatedByUserId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_UpdatedByUserId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueWcagCriteria_Issues_IssueId",
                table: "IssueWcagCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_GovernanceBoards_GovernanceBoardId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskCategories_RiskCategoryId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskImpactLevels_RiskImpactLevelId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskLikelihoods_RiskLikelihoodId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskPriorities_RiskPriorityId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskProximities_RiskProximityId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskStatuses_RiskStatusId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Service_PrimaryProductId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_ClosedByUserId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_CreatedByUserId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_OwnerUserId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_UpdatedByUserId",
                table: "Risks");

            migrationBuilder.DropTable(
                name: "ActionCategories");

            migrationBuilder.DropTable(
                name: "ActionEscalationThresholds");

            migrationBuilder.DropTable(
                name: "ActionImpactLevels");

            migrationBuilder.DropTable(
                name: "ActionPriorities");

            migrationBuilder.DropTable(
                name: "ActionReminderFrequencies");

            migrationBuilder.DropTable(
                name: "ActionStatuses");

            migrationBuilder.DropTable(
                name: "ActionTags");

            migrationBuilder.DropTable(
                name: "ActionTypes");

            migrationBuilder.DropTable(
                name: "DecisionImplementationStatuses");

            migrationBuilder.DropTable(
                name: "DecisionOutcomes");

            migrationBuilder.DropTable(
                name: "DecisionPriorities");

            migrationBuilder.DropTable(
                name: "DecisionStatuses");

            migrationBuilder.DropTable(
                name: "DecisionTags");

            migrationBuilder.DropTable(
                name: "GovernanceBoards");

            migrationBuilder.DropTable(
                name: "IssueCategories");

            migrationBuilder.DropTable(
                name: "IssuePriorities");

            migrationBuilder.DropTable(
                name: "IssueSeverities");

            migrationBuilder.DropTable(
                name: "IssueStatuses");

            migrationBuilder.DropTable(
                name: "IssueTags");

            migrationBuilder.DropTable(
                name: "RaidEvidenceTypes");

            migrationBuilder.DropTable(
                name: "RiskCategories");

            migrationBuilder.DropTable(
                name: "RiskImpactLevels");

            migrationBuilder.DropTable(
                name: "RiskLikelihoods");

            migrationBuilder.DropTable(
                name: "RiskPriorities");

            migrationBuilder.DropTable(
                name: "RiskProximities");

            migrationBuilder.DropTable(
                name: "RiskStatuses");

            migrationBuilder.DropTable(
                name: "RiskTags");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ClosedByUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_CreatedByUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_GovernanceBoardId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_OwnerUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_PrimaryProductId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskCategoryId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskImpactLevelId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskLikelihoodId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskPriorityId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskProximityId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskStatusId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_UpdatedByUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_IssueWcagCriteria_IssueId",
                table: "IssueWcagCriteria");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ClosedByUserId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_CreatedByUserId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_IssueCategoryId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_MilestoneId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_PrimaryProductId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_PriorityId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_RiskId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_SeverityId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_StatusId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_UpdatedByUserId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_IssueHistories_IssueId",
                table: "IssueHistories");

            migrationBuilder.DropIndex(
                name: "IX_IssueComments_IssueId",
                table: "IssueComments");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ClosedByUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_CreatedByUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_DecisionMakerUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_EvidenceTypeId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_GovernanceBoardId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ImplementationOwnerUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ImplementationStatusId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_MilestoneId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_OutcomeId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_PrimaryProductId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_PriorityId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ProposedByUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_SponsorUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_StatusId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_UpdatedByUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_VerifiedByUserId",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_AccountablePersonUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ActionTypeId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_AssignedToUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_CategoryId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ClosedByUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_CreatedByUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_EscalationThresholdId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_EvidenceTypeId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ImpactLevelId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_IssueId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_MilestoneId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_PrimaryProductId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_PriorityId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ReminderFrequencyId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_RiskId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_ServiceId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_StatusId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_UpdatedByUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_VerifiedByUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "GovernanceBoardId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "IdentifiedDate",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "InherentScore",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "LastReviewDate",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "PrimaryProductId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ResidualScore",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskCategoryId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskImpactLevelId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskLikelihoodId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskPriorityId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskProximityId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskStatusId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "IssueWcagCriteria");

            migrationBuilder.DropColumn(
                name: "BlocksRelease",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "IssueCategoryId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "PrimaryProductId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ResolvedDate",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "RiskId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ServiceImpactSummary",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SeverityId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "UserImpactSummary",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "IssueHistories");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "IssueComments");

            migrationBuilder.DropColumn(
                name: "AdditionalDocuments",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "AgendaItemReference",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "DecisionMakerUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "EvidenceTypeId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ExpiresDate",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "GovernanceBoardId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ImplementationDeadline",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ImplementationOwnerUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ImplementationStatusId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "OutcomeId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "PrimaryProductId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ProposedByUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ResponsibleTeam",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "SponsorUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "VerificationRequired",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "AccountablePersonUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ActionTypeId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "Blocked",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "BlockedReason",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "EscalationThresholdId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "EscalationTriggered",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "EvidenceTypeId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ImpactLevelId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "LastProgressUpdate",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "LifecyclePhase",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "PrimaryProductId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "Rag",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ReminderFrequencyId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "RiskId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "TeamName",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "VerificationRequired",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "Actions");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "Risks",
                newName: "SourceType");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Risks",
                newName: "SourceReference");

            migrationBuilder.RenameColumn(
                name: "ResponseStrategy",
                table: "Risks",
                newName: "SourceRecordUrl");
        }
    }
}
