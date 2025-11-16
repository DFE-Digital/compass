using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UseCmsBusinessAreasForLeadership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBusinessAreaRoleAssignments_BusinessAreaLookups_BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserBusinessAreaRoleAssignments_BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserBusinessAreaRoleAssignments_UserId_BusinessAreaLookupId_Role",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.AddColumn<string>(
                name: "BusinessAreaKey",
                table: "UserBusinessAreaRoleAssignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessAreaName",
                table: "UserBusinessAreaRoleAssignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
UPDATE uba
SET BusinessAreaName = COALESCE(LTRIM(RTRIM(bal.Name)), ''),
    BusinessAreaKey = LOWER(
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            COALESCE(LTRIM(RTRIM(bal.Name)), ''),
            ' ', '-'),
            '&', 'and'),
            '/', '-'),
            '\\', '-'),
            ',', '-'),
            '.', '-'),
            '--', '-')
    )
FROM UserBusinessAreaRoleAssignments uba
LEFT JOIN BusinessAreaLookups bal ON bal.Id = uba.BusinessAreaLookupId;
""");

            migrationBuilder.Sql("""
UPDATE UserBusinessAreaRoleAssignments
SET BusinessAreaName = 'Unassigned',
    BusinessAreaKey = CASE 
        WHEN BusinessAreaKey IS NULL OR BusinessAreaKey = '' THEN CONCAT('area-', Id)
        ELSE BusinessAreaKey END
WHERE BusinessAreaName IS NULL OR BusinessAreaName = '';
""");

            migrationBuilder.DropColumn(
                name: "BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinessAreaRoleAssignments_UserId_BusinessAreaKey_Role",
                table: "UserBusinessAreaRoleAssignments",
                columns: new[] { "UserId", "BusinessAreaKey", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserBusinessAreaRoleAssignments_UserId_BusinessAreaKey_Role",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.DropColumn(
                name: "BusinessAreaKey",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.DropColumn(
                name: "BusinessAreaName",
                table: "UserBusinessAreaRoleAssignments");

            migrationBuilder.AddColumn<int>(
                name: "BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinessAreaRoleAssignments_BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinessAreaRoleAssignments_UserId_BusinessAreaLookupId_Role",
                table: "UserBusinessAreaRoleAssignments",
                columns: new[] { "UserId", "BusinessAreaLookupId", "Role" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBusinessAreaRoleAssignments_BusinessAreaLookups_BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments",
                column: "BusinessAreaLookupId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
