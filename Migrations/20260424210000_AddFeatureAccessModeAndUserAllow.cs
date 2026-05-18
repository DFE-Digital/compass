using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureAccessModeAndUserAllow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: DBs repaired at startup may already have these objects before history caught up.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Features', N'AccessMode') IS NULL
                    ALTER TABLE [dbo].[Features] ADD [AccessMode] int NOT NULL CONSTRAINT [DF_Features_AccessMode_Mig] DEFAULT (1);
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Features', N'AccessMode') IS NOT NULL
                    UPDATE [dbo].[Features] SET [AccessMode] = CASE WHEN [IsActive] = 1 THEN 1 ELSE 0 END;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.FeatureUserAllows', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[FeatureUserAllows] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [FeatureId] int NOT NULL,
                        [UserId] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_FeatureUserAllows] PRIMARY KEY CLUSTERED ([Id]),
                        CONSTRAINT [FK_FeatureUserAllows_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [dbo].[Features] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_FeatureUserAllows_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE NONCLUSTERED INDEX [IX_FeatureUserAllows_FeatureId_UserId]
                        ON [dbo].[FeatureUserAllows] ([FeatureId], [UserId]);
                    CREATE NONCLUSTERED INDEX [IX_FeatureUserAllows_UserId]
                        ON [dbo].[FeatureUserAllows] ([UserId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.FeatureUserAllows', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FeatureUserAllows_FeatureId_UserId' AND object_id = OBJECT_ID(N'dbo.FeatureUserAllows'))
                    CREATE UNIQUE NONCLUSTERED INDEX [IX_FeatureUserAllows_FeatureId_UserId]
                        ON [dbo].[FeatureUserAllows] ([FeatureId], [UserId]);
                IF OBJECT_ID(N'dbo.FeatureUserAllows', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FeatureUserAllows_UserId' AND object_id = OBJECT_ID(N'dbo.FeatureUserAllows'))
                    CREATE NONCLUSTERED INDEX [IX_FeatureUserAllows_UserId]
                        ON [dbo].[FeatureUserAllows] ([UserId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureUserAllows");

            migrationBuilder.DropColumn(
                name: "AccessMode",
                table: "Features");
        }
    }
}
