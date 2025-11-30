using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingAuditLogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if columns exist before adding them (idempotent migration)
            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'AfterJson')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [AfterJson] nvarchar(max) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'BeforeJson')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [BeforeJson] nvarchar(max) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'ChangedByEmail')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [ChangedByEmail] nvarchar(320) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'ChangedByUserId')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [ChangedByUserId] nvarchar(100) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'EntityReference')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [EntityReference] nvarchar(200) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'IpAddress')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [IpAddress] nvarchar(64) NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'UserAgent')
                BEGIN
                    ALTER TABLE [AuditLogs] ADD [UserAgent] nvarchar(400) NULL;
                END
            ";

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove columns if they exist
            var sql = @"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'AfterJson')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [AfterJson];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'BeforeJson')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [BeforeJson];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'ChangedByEmail')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [ChangedByEmail];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'ChangedByUserId')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [ChangedByUserId];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'EntityReference')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [EntityReference];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'IpAddress')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [IpAddress];
                END

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'UserAgent')
                BEGIN
                    ALTER TABLE [AuditLogs] DROP COLUMN [UserAgent];
                END
            ";

            migrationBuilder.Sql(sql);
        }
    }
}
