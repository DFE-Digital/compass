using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <summary>
    /// Adds OptionsLookupKey so choice questions can bind to an admin lookup panel.
    /// </summary>
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927180000_AddCoreCensusThemeFieldOptionsLookupKey")]
    public partial class AddCoreCensusThemeFieldOptionsLookupKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OptionsLookupKey",
                table: "CoreCensusThemeFields",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Align estate list types and section-status guidance for already-seeded catalogues.
            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET FieldType = 10
                WHERE LOWER(StableKey) = N'linked-services';
                """);

            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET FieldType = 11
                WHERE LOWER(StableKey) = N'linked-service-lines';
                """);

            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET Label = N'Section information status',
                    Guidance = N'Only complete this section if none of the questions can be answered, or there is no evidence to provide.'
                WHERE LOWER(StableKey) LIKE N'%-section-status'
                   OR LOWER(StableKey) = N'section-status';
                """);

            migrationBuilder.Sql("""
                UPDATE CoreCensusThemeFields
                SET OptionsLookupKey = N'fips-user-groups'
                WHERE LOWER(StableKey) = N'primary-user-group-type';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionsLookupKey",
                table: "CoreCensusThemeFields");
        }
    }
}
