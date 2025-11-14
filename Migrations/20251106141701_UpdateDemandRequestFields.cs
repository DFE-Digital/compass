using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDemandRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DepartmentGroup",
                table: "DemandRequests",
                newName: "BusinessArea");

            migrationBuilder.AddColumn<string>(
                name: "DigitalServiceDetails",
                table: "DemandRequests",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManifestoStatutoryDetails",
                table: "DemandRequests",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformanceReportingPeriodExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReportingPeriodExclusions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingPeriodExclusions_IsActive",
                table: "PerformanceReportingPeriodExclusions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReportingPeriodExclusions_Year_Month",
                table: "PerformanceReportingPeriodExclusions",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceReportingPeriodExclusions");

            migrationBuilder.DropColumn(
                name: "DigitalServiceDetails",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "ManifestoStatutoryDetails",
                table: "DemandRequests");

            migrationBuilder.RenameColumn(
                name: "BusinessArea",
                table: "DemandRequests",
                newName: "DepartmentGroup");
        }
    }
}
