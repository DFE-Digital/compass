using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class Surveys_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "SurveyTemplates",
                columns: table => new
                {
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTemplates", x => x.SurveyTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "ScoreSnapshots",
                columns: table => new
                {
                    ScoreSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ResponsesCount = table.Column<int>(type: "int", nullable: false),
                    AvgUss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MedianUss = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreSnapshots", x => x.ScoreSnapshotId);
                    table.ForeignKey(
                        name: "FK_ScoreSnapshots_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JourneySteps",
                columns: table => new
                {
                    JourneyStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConditionalOnJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneySteps", x => x.JourneyStepId);
                    table.ForeignKey(
                        name: "FK_JourneySteps_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "SurveyTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyInstances",
                columns: table => new
                {
                    SurveyInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WeightsJson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyInstances", x => x.SurveyInstanceId);
                    table.ForeignKey(
                        name: "FK_SurveyInstances_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyInstances_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "SurveyTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    SurveyQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Mandatory = table.Column<bool>(type: "bit", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    InputType = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.SurveyQuestionId);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "SurveyTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponses",
                columns: table => new
                {
                    SurveyResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserAgentHash = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GeoRegion = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FreeText = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UssComputed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Band = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponses", x => x.SurveyResponseId);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_SurveyInstances_SurveyInstanceId",
                        column: x => x.SurveyInstanceId,
                        principalTable: "SurveyInstances",
                        principalColumn: "SurveyInstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyOptions",
                columns: table => new
                {
                    SurveyOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyOptions", x => x.SurveyOptionId);
                    table.ForeignKey(
                        name: "FK_SurveyOptions_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "SurveyQuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseAnswers",
                columns: table => new
                {
                    ResponseAnswerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OptionValue = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseAnswers", x => x.ResponseAnswerId);
                    table.ForeignKey(
                        name: "FK_ResponseAnswers_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "SurveyQuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseAnswers_SurveyResponses_SurveyResponseId",
                        column: x => x.SurveyResponseId,
                        principalTable: "SurveyResponses",
                        principalColumn: "SurveyResponseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneySteps_SurveyTemplateId_Ordinal",
                table: "JourneySteps",
                columns: new[] { "SurveyTemplateId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseAnswers_SurveyQuestionId",
                table: "ResponseAnswers",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseAnswers_SurveyResponseId",
                table: "ResponseAnswers",
                column: "SurveyResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSnapshots_ServiceId",
                table: "ScoreSnapshots",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_FipsId",
                table: "Service",
                column: "FipsId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstances_ServiceId_IsActive",
                table: "SurveyInstances",
                columns: new[] { "ServiceId", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstances_SurveyTemplateId",
                table: "SurveyInstances",
                column: "SurveyTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOptions_SurveyQuestionId",
                table: "SurveyOptions",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_SurveyTemplateId_Code",
                table: "SurveyQuestions",
                columns: new[] { "SurveyTemplateId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyInstanceId",
                table: "SurveyResponses",
                column: "SurveyInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_Name_Version",
                table: "SurveyTemplates",
                columns: new[] { "Name", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "JourneySteps");

            migrationBuilder.DropTable(
                name: "ResponseAnswers");

            migrationBuilder.DropTable(
                name: "ScoreSnapshots");

            migrationBuilder.DropTable(
                name: "SurveyOptions");

            migrationBuilder.DropTable(
                name: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "SurveyInstances");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "SurveyTemplates");
        }
    }
}
