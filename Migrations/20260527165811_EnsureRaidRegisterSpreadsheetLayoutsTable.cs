using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class EnsureRaidRegisterSpreadsheetLayoutsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table may already exist from 20260527160754_AddRaidRegisterSpreadsheetLayouts (partial deploy / history drift).
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[RaidRegisterSpreadsheetLayouts]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [RaidRegisterSpreadsheetLayouts] (
                        [Id] int NOT NULL IDENTITY,
                        [EntityType] nvarchar(32) NOT NULL,
                        [ColumnOrderJson] nvarchar(max) NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [UpdatedByUserId] int NULL,
                        CONSTRAINT [PK_RaidRegisterSpreadsheetLayouts] PRIMARY KEY ([Id])
                    );
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_RaidRegisterSpreadsheetLayouts_Users_UpdatedByUserId')
                BEGIN
                    ALTER TABLE [RaidRegisterSpreadsheetLayouts]
                    ADD CONSTRAINT [FK_RaidRegisterSpreadsheetLayouts_Users_UpdatedByUserId]
                        FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_RaidRegisterSpreadsheetLayouts_EntityType'
                      AND object_id = OBJECT_ID(N'[RaidRegisterSpreadsheetLayouts]'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_RaidRegisterSpreadsheetLayouts_EntityType]
                        ON [RaidRegisterSpreadsheetLayouts] ([EntityType]);
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_RaidRegisterSpreadsheetLayouts_UpdatedByUserId'
                      AND object_id = OBJECT_ID(N'[RaidRegisterSpreadsheetLayouts]'))
                BEGIN
                    CREATE INDEX [IX_RaidRegisterSpreadsheetLayouts_UpdatedByUserId]
                        ON [RaidRegisterSpreadsheetLayouts] ([UpdatedByUserId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidRegisterSpreadsheetLayouts");
        }
    }
}
