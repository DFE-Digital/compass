using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsCmdbSnapshotJsonAndSyncRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastCmdbSnapshotJson",
                table: "CMDBProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FipsCmdbSyncRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FieldScope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MatchKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetStatus = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsCmdbSyncRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FipsCmdbSyncRules_IsActive_SortOrder",
                table: "FipsCmdbSyncRules",
                columns: new[] { "IsActive", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FipsCmdbSyncRules");

            migrationBuilder.DropColumn(
                name: "LastCmdbSnapshotJson",
                table: "CMDBProducts");
        }
    }
}
