using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HintText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ValidFromYear = table.Column<int>(type: "int", nullable: false),
                    ValidFromMonth = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionalStandards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalStandards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HintText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ValidFromYear = table.Column<int>(type: "int", nullable: false),
                    ValidFromMonth = table.Column<int>(type: "int", nullable: false),
                    ApplicablePhases = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiTokenId = table.Column<int>(type: "int", nullable: false),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    QueryString = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestBody = table.Column<string>(type: "text", maxLength: 450, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", maxLength: 450, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_ApiTokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiTokenPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiTokenId = table.Column<int>(type: "int", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CanRead = table.Column<bool>(type: "bit", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanUpdate = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokenPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokenPermissions_ApiTokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseMetricValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnterpriseReturnId = table.Column<int>(type: "int", nullable: false),
                    EnterpriseMetricId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseMetricValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnterpriseMetricValues_EnterpriseMetrics_EnterpriseMetricId",
                        column: x => x.EnterpriseMetricId,
                        principalTable: "EnterpriseMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnterpriseMetricValues_EnterpriseReturns_EnterpriseReturnId",
                        column: x => x.EnterpriseReturnId,
                        principalTable: "EnterpriseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FunctionalStandardAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionalStandardId = table.Column<int>(type: "int", nullable: false),
                    AssessmentName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalStandardAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionalStandardAssessments_FunctionalStandards_FunctionalStandardId",
                        column: x => x.FunctionalStandardId,
                        principalTable: "FunctionalStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FunctionalStandardThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionalStandardId = table.Column<int>(type: "int", nullable: false),
                    ThemeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalStandardThemes", x => x.Id);
                    table.UniqueConstraint("AK_FunctionalStandardThemes_FunctionalStandardId_ThemeId", x => new { x.FunctionalStandardId, x.ThemeId });
                    table.ForeignKey(
                        name: "FK_FunctionalStandardThemes_FunctionalStandards_FunctionalStandardId",
                        column: x => x.FunctionalStandardId,
                        principalTable: "FunctionalStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMetricValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductReturnId = table.Column<int>(type: "int", nullable: false),
                    PerformanceMetricId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMetricValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMetricValues_PerformanceMetrics_PerformanceMetricId",
                        column: x => x.PerformanceMetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMetricValues_ProductReturns_ProductReturnId",
                        column: x => x.ProductReturnId,
                        principalTable: "ProductReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RagStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SuccessMeasures = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Objectives_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PreferredBusinessAreas = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionalStandardId = table.Column<int>(type: "int", nullable: false),
                    ThemeId = table.Column<int>(type: "int", nullable: false),
                    PracticeAreaId = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeAreas", x => x.Id);
                    table.UniqueConstraint("AK_PracticeAreas_FunctionalStandardId_ThemeId_PracticeAreaId", x => new { x.FunctionalStandardId, x.ThemeId, x.PracticeAreaId });
                    table.ForeignKey(
                        name: "FK_PracticeAreas_FunctionalStandardThemes_FunctionalStandardId_ThemeId",
                        columns: x => new { x.FunctionalStandardId, x.ThemeId },
                        principalTable: "FunctionalStandardThemes",
                        principalColumns: new[] { "FunctionalStandardId", "ThemeId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActionSourceId = table.Column<int>(type: "int", nullable: true),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ParentActionId = table.Column<int>(type: "int", nullable: true),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_ActionSources_ActionSourceId",
                        column: x => x.ActionSourceId,
                        principalTable: "ActionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actions_Actions_ParentActionId",
                        column: x => x.ParentActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actions_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DetectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ResolutionSummary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Workaround = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BlockedFlag = table.Column<bool>(type: "bit", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    OwnerEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BaselineDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    ExternalRef = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Milestones_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RiskTierId = table.Column<int>(type: "int", nullable: true),
                    OwnerEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ImpactRating = table.Column<int>(type: "int", nullable: false),
                    LikelihoodRating = table.Column<int>(type: "int", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    ProximityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResidualImpact = table.Column<int>(type: "int", nullable: true),
                    ResidualLikelihood = table.Column<int>(type: "int", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Risks_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Risks_RiskTiers_RiskTierId",
                        column: x => x.RiskTierId,
                        principalTable: "RiskTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionalStandardId = table.Column<int>(type: "int", nullable: false),
                    ThemeId = table.Column<int>(type: "int", nullable: false),
                    PracticeAreaId = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CriteriaCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Criteria = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.Id);
                    table.UniqueConstraint("AK_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode", x => new { x.FunctionalStandardId, x.ThemeId, x.PracticeAreaId, x.CriteriaCode });
                    table.ForeignKey(
                        name: "FK_Criteria_PracticeAreas_FunctionalStandardId_ThemeId_PracticeAreaId",
                        columns: x => new { x.FunctionalStandardId, x.ThemeId, x.PracticeAreaId },
                        principalTable: "PracticeAreas",
                        principalColumns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueActions",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueActions", x => new { x.IssueId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_IssueActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueActions_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneActions",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneActions", x => new { x.MilestoneId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_MilestoneActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneActions_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneIssues",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    IssueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneIssues", x => new { x.MilestoneId, x.IssueId });
                    table.ForeignKey(
                        name: "FK_MilestoneIssues_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneIssues_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneRisks",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    RiskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneRisks", x => new { x.MilestoneId, x.RiskId });
                    table.ForeignKey(
                        name: "FK_MilestoneRisks_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneRisks_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskActions",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskActions", x => new { x.RiskId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_RiskActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskActions_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskRiskTypes",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    RiskTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRiskTypes", x => new { x.RiskId, x.RiskTypeId });
                    table.ForeignKey(
                        name: "FK_RiskRiskTypes_RiskTypes_RiskTypeId",
                        column: x => x.RiskTypeId,
                        principalTable: "RiskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskRiskTypes_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentCriteriaResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    FunctionalStandardId = table.Column<int>(type: "int", nullable: false),
                    ThemeId = table.Column<int>(type: "int", nullable: false),
                    PracticeAreaId = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CriteriaCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Attainment = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriteriaResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentCriteriaResponses_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                        columns: x => new { x.FunctionalStandardId, x.ThemeId, x.PracticeAreaId, x.CriteriaCode },
                        principalTable: "Criteria",
                        principalColumns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentCriteriaResponses_FunctionalStandardAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "FunctionalStandardAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionSourceId",
                table: "Actions",
                column: "ActionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AssignedToEmail",
                table: "Actions",
                column: "AssignedToEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_DueDate",
                table: "Actions",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_FipsId",
                table: "Actions",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ObjectiveId",
                table: "Actions",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ParentActionId",
                table: "Actions",
                column: "ParentActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_Status_Priority",
                table: "Actions",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_Code",
                table: "ActionSources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_IsActive",
                table: "ActionSources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSources_SortOrder",
                table: "ActionSources",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_ApiTokenId",
                table: "ApiRequestLogs",
                column: "ApiTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_IsSuccess",
                table: "ApiRequestLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestTimestamp",
                table: "ApiRequestLogs",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenPermissions_ApiTokenId_Resource",
                table: "ApiTokenPermissions",
                columns: new[] { "ApiTokenId", "Resource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_CreatedAt",
                table: "ApiTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_ExpiresAt",
                table: "ApiTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_IsActive",
                table: "ApiTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_Token",
                table: "ApiTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteriaResponses_AssessmentId_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "AssessmentCriteriaResponses",
                columns: new[] { "AssessmentId", "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteriaResponses_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "AssessmentCriteriaResponses",
                columns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedByUserId",
                table: "Comments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetrics_Identifier",
                table: "EnterpriseMetrics",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetricValues_EnterpriseMetricId",
                table: "EnterpriseMetricValues",
                column: "EnterpriseMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetricValues_EnterpriseReturnId_EnterpriseMetricId",
                table: "EnterpriseMetricValues",
                columns: new[] { "EnterpriseReturnId", "EnterpriseMetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseReturns_Year_Month",
                table: "EnterpriseReturns",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalStandardAssessments_FunctionalStandardId",
                table: "FunctionalStandardAssessments",
                column: "FunctionalStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalStandardThemes_FunctionalStandardId_ThemeId",
                table: "FunctionalStandardThemes",
                columns: new[] { "FunctionalStandardId", "ThemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueActions_ActionId",
                table: "IssueActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_FipsId",
                table: "Issues",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ObjectiveId",
                table: "Issues",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_OwnerUserId",
                table: "Issues",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Severity_Priority",
                table: "Issues",
                columns: new[] { "Severity", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Status",
                table: "Issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_TargetResolutionDate",
                table: "Issues",
                column: "TargetResolutionDate");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneActions_ActionId",
                table: "MilestoneActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneIssues_IssueId",
                table: "MilestoneIssues",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneRisks_RiskId",
                table: "MilestoneRisks",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_DueDate",
                table: "Milestones",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_FipsId",
                table: "Milestones",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ObjectiveId",
                table: "Milestones",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_OwnerUserId",
                table: "Milestones",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_Status",
                table: "Milestones",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_OwnerUserId",
                table: "Objectives",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_RagStatus",
                table: "Objectives",
                column: "RagStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_Status",
                table: "Objectives",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Identifier",
                table: "PerformanceMetrics",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMetricValues_PerformanceMetricId",
                table: "ProductMetricValues",
                column: "PerformanceMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMetricValues_ProductReturnId_PerformanceMetricId",
                table: "ProductMetricValues",
                columns: new[] { "ProductReturnId", "PerformanceMetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReturns_FipsId_Year_Month",
                table: "ProductReturns",
                columns: new[] { "FipsId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskActions_ActionId",
                table: "RiskActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRiskTypes_RiskTypeId",
                table: "RiskRiskTypes",
                column: "RiskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_FipsId",
                table: "Risks",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ObjectiveId",
                table: "Risks",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ProximityDate",
                table: "Risks",
                column: "ProximityDate");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskScore",
                table: "Risks",
                column: "RiskScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskTierId",
                table: "Risks",
                column: "RiskTierId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_Status",
                table: "Risks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_Code",
                table: "RiskTiers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_IsActive",
                table: "RiskTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_SortOrder",
                table: "RiskTiers",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTypes_Code",
                table: "RiskTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskTypes_IsActive",
                table: "RiskTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "ApiTokenPermissions");

            migrationBuilder.DropTable(
                name: "AssessmentCriteriaResponses");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "EnterpriseMetricValues");

            migrationBuilder.DropTable(
                name: "IssueActions");

            migrationBuilder.DropTable(
                name: "MilestoneActions");

            migrationBuilder.DropTable(
                name: "MilestoneIssues");

            migrationBuilder.DropTable(
                name: "MilestoneRisks");

            migrationBuilder.DropTable(
                name: "ProductMetricValues");

            migrationBuilder.DropTable(
                name: "RiskActions");

            migrationBuilder.DropTable(
                name: "RiskRiskTypes");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropTable(
                name: "FunctionalStandardAssessments");

            migrationBuilder.DropTable(
                name: "EnterpriseMetrics");

            migrationBuilder.DropTable(
                name: "EnterpriseReturns");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "ProductReturns");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "RiskTypes");

            migrationBuilder.DropTable(
                name: "Risks");

            migrationBuilder.DropTable(
                name: "PracticeAreas");

            migrationBuilder.DropTable(
                name: "ActionSources");

            migrationBuilder.DropTable(
                name: "Objectives");

            migrationBuilder.DropTable(
                name: "RiskTiers");

            migrationBuilder.DropTable(
                name: "FunctionalStandardThemes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "FunctionalStandards");
        }
    }
}
