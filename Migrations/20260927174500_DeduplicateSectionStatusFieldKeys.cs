using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <summary>
    /// Core census themes each had a field with StableKey <c>section-status</c>.
    /// Flattening the shared catalogue for census forms then threw on ToDictionary.
    /// Rename to <c>{themeKey}-section-status</c> and remap FieldId-backed answers.
    /// </summary>
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927174500_DeduplicateSectionStatusFieldKeys")]
    public partial class DeduplicateSectionStatusFieldKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename catalogue fields first.
            migrationBuilder.Sql("""
                UPDATE f
                SET f.StableKey = LOWER(t.StableKey) + N'-section-status'
                FROM CoreCensusThemeFields f
                INNER JOIN CoreCensusThemes t ON t.Id = f.CoreCensusThemeId
                WHERE LOWER(f.StableKey) = N'section-status';
                """);

            // Remap answers that still point at the renamed field by FieldId.
            migrationBuilder.Sql("""
                UPDATE a
                SET a.FieldStableKey = f.StableKey
                FROM ServiceDataModelAnswers a
                INNER JOIN CoreCensusThemeFields f ON f.Id = a.ServiceDataModelFieldId
                WHERE LOWER(f.StableKey) LIKE N'%-section-status'
                  AND (a.FieldStableKey IS NULL OR LOWER(a.FieldStableKey) = N'section-status'
                       OR a.FieldStableKey <> f.StableKey);
                """);

            migrationBuilder.Sql("""
                UPDATE p
                SET p.FieldStableKey = f.StableKey
                FROM ServiceDataModelProposedRegisterChanges p
                INNER JOIN CoreCensusThemeFields f ON f.Id = p.ServiceDataModelFieldId
                WHERE LOWER(f.StableKey) LIKE N'%-section-status'
                  AND (p.FieldStableKey IS NULL OR LOWER(p.FieldStableKey) = N'section-status'
                       OR p.FieldStableKey <> f.StableKey);
                """);

            // FieldStableKey-only orphans (no FieldId): keep the value on the first theme by sort.
            migrationBuilder.Sql("""
                ;WITH FirstKey AS (
                    SELECT TOP 1 LOWER(t.StableKey) + N'-section-status' AS NewKey
                    FROM CoreCensusThemes t
                    WHERE t.IsServiceOffering = 0
                    ORDER BY t.SortOrder, t.Name
                )
                UPDATE a
                SET a.FieldStableKey = fk.NewKey
                FROM ServiceDataModelAnswers a
                CROSS JOIN FirstKey fk
                WHERE LOWER(a.FieldStableKey) = N'section-status'
                  AND a.ServiceDataModelFieldId IS NULL;
                """);

            migrationBuilder.Sql("""
                ;WITH FirstKey AS (
                    SELECT TOP 1 LOWER(t.StableKey) + N'-section-status' AS NewKey
                    FROM CoreCensusThemes t
                    WHERE t.IsServiceOffering = 0
                    ORDER BY t.SortOrder, t.Name
                )
                UPDATE p
                SET p.FieldStableKey = fk.NewKey
                FROM ServiceDataModelProposedRegisterChanges p
                CROSS JOIN FirstKey fk
                WHERE LOWER(p.FieldStableKey) = N'section-status'
                  AND p.ServiceDataModelFieldId IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reverse: only keys that match the theme-prefixed pattern.
            migrationBuilder.Sql("""
                UPDATE f
                SET f.StableKey = N'section-status'
                FROM CoreCensusThemeFields f
                INNER JOIN CoreCensusThemes t ON t.Id = f.CoreCensusThemeId
                WHERE LOWER(f.StableKey) = LOWER(t.StableKey) + N'-section-status';
                """);

            migrationBuilder.Sql("""
                UPDATE a
                SET a.FieldStableKey = N'section-status'
                FROM ServiceDataModelAnswers a
                INNER JOIN CoreCensusThemeFields f ON f.Id = a.ServiceDataModelFieldId
                WHERE LOWER(f.StableKey) = N'section-status'
                  AND a.FieldStableKey IS NOT NULL
                  AND LOWER(a.FieldStableKey) LIKE N'%-section-status';
                """);

            migrationBuilder.Sql("""
                UPDATE p
                SET p.FieldStableKey = N'section-status'
                FROM ServiceDataModelProposedRegisterChanges p
                INNER JOIN CoreCensusThemeFields f ON f.Id = p.ServiceDataModelFieldId
                WHERE LOWER(f.StableKey) = N'section-status'
                  AND p.FieldStableKey IS NOT NULL
                  AND LOWER(p.FieldStableKey) LIKE N'%-section-status';
                """);
        }
    }
}
