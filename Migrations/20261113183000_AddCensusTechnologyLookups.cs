using System;
using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261113183000_AddCensusTechnologyLookups")]
    public class AddCensusTechnologyLookups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CensusTechnologyLookups",
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
                    table.PrimaryKey("PK_CensusTechnologyLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CensusTechnologyLookups_Name",
                table: "CensusTechnologyLookups",
                column: "Name");

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM CensusTechnologyLookups)
                BEGIN
                    INSERT INTO CensusTechnologyLookups (Name, SortOrder, IsActive, CreatedAt, UpdatedAt)
                    VALUES
                        (N'Ruby', 10, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Node.js', 20, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Python', 30, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Java', 40, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Spring', 50, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'.NET / C#', 60, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'VB', 70, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'F#', 80, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'ASP.NET Core (Razor / Blazor)', 90, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Go', 100, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'PHP', 110, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'R Shiny', 120, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'React', 130, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Angular', 140, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Vue', 150, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Next.js', 160, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'MS / Azure SQL', 170, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Postgres', 180, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Mongo', 190, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Redis', 200, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Oracle', 210, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Cosmos DB', 220, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Power Platform', 230, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Salesforce', 240, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Docker', 250, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Kubernetes', 260, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Other language', 270, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Other database', 280, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                        (N'Other', 290, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CensusTechnologyLookups");
        }
    }
}
