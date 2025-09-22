using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RagStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AllocatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MeasurementType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AllowNotApplicable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MilestoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RagStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NewDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestoneUpdates_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportingMetricId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CategoryValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricConditions_ReportingMetrics_ReportingMetricId",
                        column: x => x.ReportingMetricId,
                        principalTable: "ReportingMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportingData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MetricId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ReportingPeriod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SubmittedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReportingMetricId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingData_ReportingMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "ReportingMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportingData_ReportingMetrics_ReportingMetricId",
                        column: x => x.ReportingMetricId,
                        principalTable: "ReportingMetrics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricConditions_ReportingMetricId",
                table: "MetricConditions",
                column: "ReportingMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneUpdates_MilestoneId",
                table: "MilestoneUpdates",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAllocations_ProductId_UserEmail",
                table: "ProductAllocations",
                columns: new[] { "ProductId", "UserEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingData_MetricId",
                table: "ReportingData",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingData_ReportingMetricId",
                table: "ReportingData",
                column: "ReportingMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingUsers_Email",
                table: "ReportingUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricConditions");

            migrationBuilder.DropTable(
                name: "MilestoneUpdates");

            migrationBuilder.DropTable(
                name: "ProductAllocations");

            migrationBuilder.DropTable(
                name: "ReportingData");

            migrationBuilder.DropTable(
                name: "ReportingUsers");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "ReportingMetrics");
        }
    }
}
