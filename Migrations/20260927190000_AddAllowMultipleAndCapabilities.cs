using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <summary>
    /// AllowMultiple on shared census fields, Lookup field type migration for seeded lookups,
    /// and CapabilityLookups admin catalogue.
    /// </summary>
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927190000_AddAllowMultipleAndCapabilities")]
    public partial class AddAllowMultipleAndCapabilities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = N'CoreCensusThemeFields' AND COLUMN_NAME = N'AllowMultiple')
                BEGIN
                    ALTER TABLE CoreCensusThemeFields
                    ADD AllowMultiple bit NOT NULL CONSTRAINT DF_CoreCensusThemeFields_AllowMultiple DEFAULT (0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.CapabilityLookups', N'U') IS NULL
                BEGIN
                    CREATE TABLE CapabilityLookups (
                        Id uniqueidentifier NOT NULL,
                        Title nvarchar(200) NOT NULL,
                        Description nvarchar(2000) NULL,
                        Reference nvarchar(100) NOT NULL,
                        SortOrder int NOT NULL,
                        IsActive bit NOT NULL,
                        CreatedUtc datetime2 NOT NULL,
                        UpdatedUtc datetime2 NOT NULL,
                        CONSTRAINT PK_CapabilityLookups PRIMARY KEY (Id)
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_CapabilityLookups_Reference'
                      AND object_id = OBJECT_ID(N'dbo.CapabilityLookups'))
                BEGIN
                    CREATE UNIQUE INDEX IX_CapabilityLookups_Reference
                    ON CapabilityLookups (Reference);
                END
                """);

            // Preserve MultipleChoice as AllowMultiple before converting to Lookup.
            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET AllowMultiple = 1
                WHERE LOWER(StableKey) = N'primary-user-group-type'
                  AND FieldType = 6;
                """);

            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET FieldType = 12,
                    OptionsLookupKey = COALESCE(NULLIF(LTRIM(RTRIM(OptionsLookupKey)), N''), N'fips-user-groups')
                WHERE LOWER(StableKey) = N'primary-user-group-type'
                  AND FieldType IN (5, 6, 12);
                """);

            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET FieldType = 12,
                    AllowMultiple = 1,
                    OptionsLookupKey = COALESCE(NULLIF(LTRIM(RTRIM(OptionsLookupKey)), N''), N'capabilities')
                WHERE LOWER(StableKey) = N'capabilities'
                  AND FieldType IN (0, 1, 5, 6, 12);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = N'CoreCensusThemeFields' AND COLUMN_NAME = N'AllowMultiple')
                BEGIN
                    ALTER TABLE CoreCensusThemeFields DROP CONSTRAINT DF_CoreCensusThemeFields_AllowMultiple;
                    ALTER TABLE CoreCensusThemeFields DROP COLUMN AllowMultiple;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.CapabilityLookups', N'U') IS NOT NULL
                    DROP TABLE CapabilityLookups;
                """);
        }
    }
}
