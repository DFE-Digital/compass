using System;
using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260423120000_AddCompassNotificationManagement")]
    public partial class AddCompassNotificationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: safe if startup repair (<see cref="Program"/>) already created these tables.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.CompassNotificationSettings', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[CompassNotificationSettings] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [EventKey] nvarchar(100) NOT NULL,
                        [IsEnabled] bit NOT NULL,
                        [RecipientFlags] int NOT NULL,
                        [UpdatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_CompassNotificationSettings] PRIMARY KEY CLUSTERED ([Id])
                    );
                END
                IF OBJECT_ID(N'dbo.CompassNotificationSettings', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompassNotificationSettings_EventKey' AND object_id = OBJECT_ID(N'dbo.CompassNotificationSettings'))
                    CREATE UNIQUE INDEX [IX_CompassNotificationSettings_EventKey] ON [dbo].[CompassNotificationSettings]([EventKey]);

                IF OBJECT_ID(N'dbo.CompassNotificationEmailLogs', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[CompassNotificationEmailLogs] (
                        [Id] bigint NOT NULL IDENTITY(1,1),
                        [SentAtUtc] datetime2 NOT NULL,
                        [RecipientEmail] nvarchar(256) NOT NULL,
                        [RecipientName] nvarchar(256) NULL,
                        [EventKey] nvarchar(100) NOT NULL,
                        [Subject] nvarchar(500) NOT NULL,
                        [Body] nvarchar(max) NOT NULL,
                        [ContextReference] nvarchar(200) NULL,
                        [SendSucceeded] bit NOT NULL,
                        [ErrorMessage] nvarchar(2000) NULL,
                        CONSTRAINT [PK_CompassNotificationEmailLogs] PRIMARY KEY CLUSTERED ([Id])
                    );
                END
                IF OBJECT_ID(N'dbo.CompassNotificationEmailLogs', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompassNotificationEmailLogs_EventKey' AND object_id = OBJECT_ID(N'dbo.CompassNotificationEmailLogs'))
                    CREATE NONCLUSTERED INDEX [IX_CompassNotificationEmailLogs_EventKey] ON [dbo].[CompassNotificationEmailLogs]([EventKey]);
                IF OBJECT_ID(N'dbo.CompassNotificationEmailLogs', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompassNotificationEmailLogs_SentAtUtc' AND object_id = OBJECT_ID(N'dbo.CompassNotificationEmailLogs'))
                    CREATE NONCLUSTERED INDEX [IX_CompassNotificationEmailLogs_SentAtUtc] ON [dbo].[CompassNotificationEmailLogs]([SentAtUtc]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.CompassNotificationEmailLogs', N'U') IS NOT NULL
                    DROP TABLE [dbo].[CompassNotificationEmailLogs];
                IF OBJECT_ID(N'dbo.CompassNotificationSettings', N'U') IS NOT NULL
                    DROP TABLE [dbo].[CompassNotificationSettings];
                """);
        }
    }
}
