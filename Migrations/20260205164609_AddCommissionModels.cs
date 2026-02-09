using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OpenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionId = table.Column<int>(type: "int", nullable: false),
                    ProductDocumentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProductTitle = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionSubmissions_Commissions_CommissionId",
                        column: x => x.CommissionId,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionMetricValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionSubmissionId = table.Column<int>(type: "int", nullable: false),
                    PerformanceMetricId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsNotCaptured = table.Column<bool>(type: "bit", nullable: false),
                    NotCapturedReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReasonForDifference = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionMetricValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionMetricValues_CommissionSubmissions_CommissionSubmissionId",
                        column: x => x.CommissionSubmissionId,
                        principalTable: "CommissionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionMetricValues_PerformanceMetrics_PerformanceMetricId",
                        column: x => x.PerformanceMetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionMetricValues_CommissionSubmissionId_PerformanceMetricId",
                table: "CommissionMetricValues",
                columns: new[] { "CommissionSubmissionId", "PerformanceMetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionMetricValues_PerformanceMetricId",
                table: "CommissionMetricValues",
                column: "PerformanceMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_IsActive",
                table: "Commissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionSubmissions_CommissionId_ProductDocumentId",
                table: "CommissionSubmissions",
                columns: new[] { "CommissionId", "ProductDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionSubmissions_FipsId",
                table: "CommissionSubmissions",
                column: "FipsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionMetricValues");

            migrationBuilder.DropTable(
                name: "CommissionSubmissions");

            migrationBuilder.DropTable(
                name: "Commissions");
        }
    }
}
