using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProjectBusinessAreaAndPhaseToUseLookupIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new ID columns (nullable initially)
            migrationBuilder.AddColumn<int>(
                name: "BusinessAreaId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhaseId",
                table: "Projects",
                type: "int",
                nullable: true);

            // Step 2: Migrate BusinessArea data from text to ID
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.BusinessAreaId = bal.Id
                FROM Projects p
                INNER JOIN BusinessAreaLookups bal ON bal.Name = p.BusinessArea AND bal.IsActive = 1
                WHERE p.BusinessArea IS NOT NULL AND p.BusinessArea != ''
            ");

            // Step 3: Migrate Phase data from text to ID
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.PhaseId = pl.Id
                FROM Projects p
                INNER JOIN PhaseLookups pl ON pl.Name = p.Phase AND pl.IsActive = 1
                WHERE p.Phase IS NOT NULL AND p.Phase != ''
            ");

            // Step 4: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Projects_BusinessAreaId",
                table: "Projects",
                column: "BusinessAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PhaseId",
                table: "Projects",
                column: "PhaseId");

            // Step 5: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Projects_BusinessAreaLookups_BusinessAreaId",
                table: "Projects",
                column: "BusinessAreaId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_PhaseLookups_PhaseId",
                table: "Projects",
                column: "PhaseId",
                principalTable: "PhaseLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Step 6: Drop old string columns
            migrationBuilder.DropColumn(
                name: "BusinessArea",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_BusinessAreaLookups_BusinessAreaId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_PhaseLookups_PhaseId",
                table: "Projects");

            // Step 2: Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Projects_BusinessAreaId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PhaseId",
                table: "Projects");

            // Step 3: Add back old string columns
            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Step 4: Migrate data back from IDs to text
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.BusinessArea = bal.Name
                FROM Projects p
                INNER JOIN BusinessAreaLookups bal ON bal.Id = p.BusinessAreaId
                WHERE p.BusinessAreaId IS NOT NULL
            ");

            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Phase = pl.Name
                FROM Projects p
                INNER JOIN PhaseLookups pl ON pl.Id = p.PhaseId
                WHERE p.PhaseId IS NOT NULL
            ");

            // Step 5: Drop ID columns
            migrationBuilder.DropColumn(
                name: "BusinessAreaId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PhaseId",
                table: "Projects");
        }
    }
}
