using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceBandsLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceBandLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MinFte = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    MaxFte = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceBandLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBandLookups_IsActive",
                table: "ResourceBandLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBandLookups_Name",
                table: "ResourceBandLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBandLookups_SortOrder",
                table: "ResourceBandLookups",
                column: "SortOrder");

            migrationBuilder.InsertData(
                table: "ResourceBandLookups",
                columns: new[] { "Name", "Description", "MinFte", "MaxFte", "CssClass", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Band 1", "Default band: 0.01 to 5 FTE.", 0.01m, 5.00m, "green", 10, true, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "Band 2", "Default band: 5.01 to 10 FTE.", 5.01m, 10.00m, "blue", 20, true, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "Band 3", "Default band: 10.01 to 20 FTE.", 10.01m, 20.00m, "teal", 30, true, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "Band 4", "Default band: 20.01 to 50 FTE.", 20.01m, 50.00m, "orange", 40, true, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "Band 5", "Default band: above 50.01 FTE.", 50.01m, null, "red", 50, true, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceBandLookups");
        }
    }
}
