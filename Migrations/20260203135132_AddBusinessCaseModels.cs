using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCaseModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    RequestorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestorName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessArea = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCaseDdtFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    FeedbackProviderEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FeedbackProviderName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCaseDdtFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessCaseDdtFeedbacks_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCaseProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: false),
                    ProductFipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCaseProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessCaseProducts_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCaseProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCaseProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessCaseProjects_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessCaseProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCaseReviewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCaseId = table.Column<int>(type: "int", nullable: false),
                    ReviewerEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewerName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewerRole = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCaseReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessCaseReviewers_BusinessCases_BusinessCaseId",
                        column: x => x.BusinessCaseId,
                        principalTable: "BusinessCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCaseDdtFeedbacks_BusinessCaseId",
                table: "BusinessCaseDdtFeedbacks",
                column: "BusinessCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCaseProducts_BusinessCaseId_ProductFipsId",
                table: "BusinessCaseProducts",
                columns: new[] { "BusinessCaseId", "ProductFipsId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCaseProjects_BusinessCaseId_ProjectId",
                table: "BusinessCaseProjects",
                columns: new[] { "BusinessCaseId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCaseProjects_ProjectId",
                table: "BusinessCaseProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCaseReviewers_BusinessCaseId",
                table: "BusinessCaseReviewers",
                column: "BusinessCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCases_BusinessArea",
                table: "BusinessCases",
                column: "BusinessArea");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCases_BusinessCaseId",
                table: "BusinessCases",
                column: "BusinessCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCases_RequestorEmail",
                table: "BusinessCases",
                column: "RequestorEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessCaseDdtFeedbacks");

            migrationBuilder.DropTable(
                name: "BusinessCaseProducts");

            migrationBuilder.DropTable(
                name: "BusinessCaseProjects");

            migrationBuilder.DropTable(
                name: "BusinessCaseReviewers");

            migrationBuilder.DropTable(
                name: "BusinessCases");
        }
    }
}
