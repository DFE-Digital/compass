using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialYearToTrainingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinancialYear",
                table: "TrainingRequests",
                type: "int",
                nullable: true);

            // Populate FinancialYear for existing records based on PlannedDate or CreatedAt
            // UK financial year starts 1st April
            migrationBuilder.Sql(@"
                UPDATE TrainingRequests
                SET FinancialYear = CASE
                    WHEN PlannedDate IS NOT NULL THEN
                        CASE 
                            WHEN MONTH(PlannedDate) >= 4 THEN YEAR(PlannedDate)
                            ELSE YEAR(PlannedDate) - 1
                        END
                    ELSE
                        CASE 
                            WHEN MONTH(CreatedAt) >= 4 THEN YEAR(CreatedAt)
                            ELSE YEAR(CreatedAt) - 1
                        END
                END
                WHERE FinancialYear IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinancialYear",
                table: "TrainingRequests");
        }
    }
}
