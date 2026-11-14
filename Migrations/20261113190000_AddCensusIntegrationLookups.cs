using System;
using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261113190000_AddCensusIntegrationLookups")]
    public class AddCensusIntegrationLookups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CensusIntegrationLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CensusIntegrationLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CensusIntegrationLookups_Name",
                table: "CensusIntegrationLookups",
                column: "Name");

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM CensusIntegrationLookups)
                BEGIN
                    INSERT INTO CensusIntegrationLookups (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
                    VALUES
                        (N'GOV.UK Notify', 10, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'GOV.UK Pay', 20, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'GOV.UK Forms', 30, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'GOV.UK One Login', 40, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'DfE Sign-in', 50, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Entra ID', 60, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'3rd Party authentication (Google)', 70, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'DfE Identity', 80, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CensusIntegrationLookups");
        }
    }
}
