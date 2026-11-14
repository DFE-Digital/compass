using System;
using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261113140000_AddServiceCensusWorkspace")]
    public class AddServiceCensusWorkspace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceCensusResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ServiceLineDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AccessibilityNotEnrolledReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCensusResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCensusResponses_CMDBProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCensusContactReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CmdbName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CmdbEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    IsIncorrect = table.Column<bool>(type: "bit", nullable: false),
                    IncorrectReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCensusContactReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCensusContactReviews_ServiceCensusResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "ServiceCensusResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCensusTaskStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CannotAnswer = table.Column<bool>(type: "bit", nullable: false),
                    CannotAnswerReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCensusTaskStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCensusTaskStates_ServiceCensusResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "ServiceCensusResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCensusAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Narrative = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LookupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SecondaryLookupCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCensusAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCensusAnswers_ServiceCensusTaskStates_TaskStateId",
                        column: x => x.TaskStateId,
                        principalTable: "ServiceCensusTaskStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCensusResponses_ProductId",
                table: "ServiceCensusResponses",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCensusContactReviews_ResponseId_RoleKey",
                table: "ServiceCensusContactReviews",
                columns: new[] { "ResponseId", "RoleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCensusTaskStates_ResponseId_TaskKey",
                table: "ServiceCensusTaskStates",
                columns: new[] { "ResponseId", "TaskKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCensusAnswers_TaskStateId",
                table: "ServiceCensusAnswers",
                column: "TaskStateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ServiceCensusAnswers");
            migrationBuilder.DropTable(name: "ServiceCensusContactReviews");
            migrationBuilder.DropTable(name: "ServiceCensusTaskStates");
            migrationBuilder.DropTable(name: "ServiceCensusResponses");
        }
    }
}
