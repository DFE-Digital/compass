using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDirectoratesToUseDivisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 2: Drop indexes that depend on DirectorateLookupId
            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 3: Add a temporary column for the new foreign key
            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: true);

            // Step 4: Migrate data: Map DirectorateLookupId to DivisionId by matching names
            migrationBuilder.Sql(@"
                UPDATE pd
                SET pd.DivisionId = d.Id
                FROM ProjectDirectorates pd
                INNER JOIN DirectorateLookups dl ON dl.Id = pd.DirectorateLookupId
                INNER JOIN Divisions d ON d.Name = dl.Name AND d.IsActive = 1
            ");

            // Step 5: Delete any ProjectDirectorates that couldn't be mapped (no matching Division)
            migrationBuilder.Sql(@"
                DELETE FROM ProjectDirectorates
                WHERE DivisionId IS NULL
            ");

            // Step 6: Make the column NOT NULL now that we've cleaned up
            migrationBuilder.AlterColumn<int>(
                name: "DivisionId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Step 7: Drop the old column
            migrationBuilder.DropColumn(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 8: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId_DivisionId",
                table: "ProjectDirectorates",
                columns: new[] { "ProjectId", "DivisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_DivisionId",
                table: "ProjectDirectorates",
                column: "DivisionId");

            // Step 9: Add the new foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_Divisions_DivisionId",
                table: "ProjectDirectorates",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_Divisions_DivisionId",
                table: "ProjectDirectorates");

            // Step 2: Drop indexes that depend on DivisionId
            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_ProjectId_DivisionId",
                table: "ProjectDirectorates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_DivisionId",
                table: "ProjectDirectorates");

            // Step 3: Add a temporary column for the old foreign key
            migrationBuilder.AddColumn<int>(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: true);

            // Step 4: Migrate data: Map DivisionId to DirectorateLookupId by matching names
            migrationBuilder.Sql(@"
                UPDATE pd
                SET pd.DirectorateLookupId = dl.Id
                FROM ProjectDirectorates pd
                INNER JOIN Divisions d ON d.Id = pd.DivisionId
                INNER JOIN DirectorateLookups dl ON dl.Name = d.Name AND dl.IsActive = 1
            ");

            // Step 5: Delete any ProjectDirectorates that couldn't be mapped (no matching DirectorateLookup)
            migrationBuilder.Sql(@"
                DELETE FROM ProjectDirectorates
                WHERE DirectorateLookupId IS NULL
            ");

            // Step 6: Make the column NOT NULL now that we've cleaned up
            migrationBuilder.AlterColumn<int>(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Step 7: Drop the DivisionId column
            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "ProjectDirectorates");

            // Step 8: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates",
                columns: new[] { "ProjectId", "DirectorateLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId");

            // Step 9: Add the old foreign key constraint back
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId",
                principalTable: "DirectorateLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
