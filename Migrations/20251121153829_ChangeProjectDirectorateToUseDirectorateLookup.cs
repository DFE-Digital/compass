using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProjectDirectorateToUseDirectorateLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_BusinessAreaLookups_BusinessAreaLookupId",
                table: "ProjectDirectorates");

            // Step 2: Drop indexes that depend on BusinessAreaLookupId
            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_ProjectId_BusinessAreaLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_BusinessAreaLookupId",
                table: "ProjectDirectorates");

            // Step 3: Add a temporary column for the new foreign key
            migrationBuilder.AddColumn<int>(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: true);

            // Step 4: Migrate data: Map BusinessAreaLookupId to DirectorateLookupId by matching names
            migrationBuilder.Sql(@"
                UPDATE pd
                SET pd.DirectorateLookupId = dl.Id
                FROM ProjectDirectorates pd
                INNER JOIN BusinessAreaLookups bal ON bal.Id = pd.BusinessAreaLookupId
                INNER JOIN DirectorateLookups dl ON dl.Name = bal.Name AND dl.IsActive = 1
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

            // Step 7: Drop the old column
            migrationBuilder.DropColumn(
                name: "BusinessAreaLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates",
                columns: new[] { "ProjectId", "DirectorateLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId");

            // Step 8: Add the new foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId",
                principalTable: "DirectorateLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 2: Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 3: Add temporary column for BusinessAreaLookupId
            migrationBuilder.AddColumn<int>(
                name: "BusinessAreaLookupId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: true);

            // Step 4: Migrate data back: Map DirectorateLookupId to BusinessAreaLookupId by matching names
            migrationBuilder.Sql(@"
                UPDATE pd
                SET pd.BusinessAreaLookupId = bal.Id
                FROM ProjectDirectorates pd
                INNER JOIN DirectorateLookups dl ON dl.Id = pd.DirectorateLookupId
                INNER JOIN BusinessAreaLookups bal ON bal.Name = dl.Name AND bal.IsActive = 1
            ");

            // Step 5: Delete any ProjectDirectorates that couldn't be mapped
            migrationBuilder.Sql(@"
                DELETE FROM ProjectDirectorates
                WHERE BusinessAreaLookupId IS NULL
            ");

            // Step 6: Make the column NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "BusinessAreaLookupId",
                table: "ProjectDirectorates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Step 7: Drop the DirectorateLookupId column
            migrationBuilder.DropColumn(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates");

            // Step 8: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_ProjectId_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                columns: new[] { "ProjectId", "BusinessAreaLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectorates_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                column: "BusinessAreaLookupId");

            // Step 9: Add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_BusinessAreaLookups_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                column: "BusinessAreaLookupId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
