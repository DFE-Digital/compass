using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <summary>
    /// Per-assignment theme complete flags for census task-list (keyed by theme stable key).
    /// </summary>
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927192000_AddServiceDataModelThemeCompletions")]
    public partial class AddServiceDataModelThemeCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.ServiceDataModelThemeCompletions', N'U') IS NULL
                BEGIN
                    CREATE TABLE ServiceDataModelThemeCompletions (
                        Id uniqueidentifier NOT NULL,
                        ServiceDataModelAssignmentId uniqueidentifier NOT NULL,
                        ThemeStableKey nvarchar(100) NOT NULL,
                        CompletedUtc datetime2 NOT NULL,
                        CompletedByEmail nvarchar(320) NULL,
                        CONSTRAINT PK_ServiceDataModelThemeCompletions PRIMARY KEY (Id),
                        CONSTRAINT FK_ServiceDataModelThemeCompletions_Assignments
                            FOREIGN KEY (ServiceDataModelAssignmentId)
                            REFERENCES ServiceDataModelAssignments (Id)
                            ON DELETE CASCADE
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_ServiceDataModelThemeCompletions_Assignment_Theme'
                      AND object_id = OBJECT_ID(N'dbo.ServiceDataModelThemeCompletions'))
                BEGIN
                    CREATE UNIQUE INDEX IX_ServiceDataModelThemeCompletions_Assignment_Theme
                    ON ServiceDataModelThemeCompletions (ServiceDataModelAssignmentId, ThemeStableKey);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.ServiceDataModelThemeCompletions', N'U') IS NOT NULL
                    DROP TABLE ServiceDataModelThemeCompletions;
                """);
        }
    }
}
