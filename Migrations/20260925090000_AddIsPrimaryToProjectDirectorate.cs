using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(Compass.Data.CompassDbContext))]
    [Migration("20260925090000_AddIsPrimaryToProjectDirectorate")]
    public class AddIsPrimaryToProjectDirectorate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "ProjectDirectorates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Designate one primary Directorate per project (earliest CreatedAt, then Id).
            // Surplus rows remain with IsPrimary=0 so historical multi-select values are not discarded.
            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY ProjectId
                               ORDER BY CreatedAt ASC, Id ASC
                           ) AS rn
                    FROM ProjectDirectorates
                )
                UPDATE pd
                SET IsPrimary = CASE WHEN r.rn = 1 THEN 1 ELSE 0 END
                FROM ProjectDirectorates pd
                INNER JOIN Ranked r ON r.Id = pd.Id;
            ");

            // Recoverable audit: log projects that had multiple directorates before single-select.
            migrationBuilder.Sql(@"
                INSERT INTO AuditLogs (AuditLogId, Entity, EntityId, Action, ChangedBy, ChangedByEmail, ChangedUtc, AfterJson)
                SELECT
                    NEWID(),
                    'Project',
                    CAST(ProjectId AS nvarchar(100)),
                    'migrate',
                    'AddIsPrimaryToProjectDirectorate',
                    'system@migration',
                    SYSUTCDATETIME(),
                    CONCAT(
                        '{""note"":""Surplus directorates retained with IsPrimary=false"",""primaryDivisionId"":',
                        MAX(CASE WHEN IsPrimary = 1 THEN DivisionId END),
                        ',""surplusDivisionIds"":""',
                        STRING_AGG(CASE WHEN IsPrimary = 0 THEN CAST(DivisionId AS nvarchar(20)) END, ','),
                        '""}'
                    )
                FROM ProjectDirectorates
                GROUP BY ProjectId
                HAVING COUNT(*) > 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "ProjectDirectorates");
        }
    }
}
