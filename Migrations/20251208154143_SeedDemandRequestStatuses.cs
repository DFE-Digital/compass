using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemandRequestStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var statuses = new[]
            {
                new { Code = "NEW", Label = "New", SortOrder = 10 },
                new { Code = "NOT_STARTED", Label = "Not started", SortOrder = 20 },
                new { Code = "ASSIGNED", Label = "Assigned", SortOrder = 30 },
                new { Code = "DDT_DIVISION_ASSIGNED", Label = "DDT Division Assigned", SortOrder = 40 },
                new { Code = "IN_DELIVERY", Label = "In delivery", SortOrder = 50 },
                new { Code = "REJECTED", Label = "Rejected", SortOrder = 60 },
                new { Code = "WITHDRAWN", Label = "Withdrawn", SortOrder = 70 },
                new { Code = "DELIVERED", Label = "Delivered", SortOrder = 80 },
                new { Code = "CLOSED", Label = "Closed", SortOrder = 90 }
            };

            foreach (var status in statuses)
            {
                var escapedLabel = status.Label.Replace("'", "''");
                migrationBuilder.Sql($@"
                    IF NOT EXISTS (SELECT 1 FROM DemandRequestStatuses WHERE Code = '{status.Code}' OR Label = '{escapedLabel}')
                    BEGIN
                        INSERT INTO DemandRequestStatuses (Code, Label, Description, SortOrder, IsActive, CreatedAt, UpdatedAt)
                        VALUES ('{status.Code}', '{escapedLabel}', NULL, {status.SortOrder}, 1, '{now}', '{now}')
                    END
                    ELSE
                    BEGIN
                        UPDATE DemandRequestStatuses
                        SET SortOrder = {status.SortOrder}, UpdatedAt = '{now}'
                        WHERE Code = '{status.Code}' OR Label = '{escapedLabel}'
                    END
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded statuses if needed
            migrationBuilder.Sql(@"
                DELETE FROM DemandRequestStatuses 
                WHERE Code IN ('NEW', 'NOT_STARTED', 'ASSIGNED', 'DDT_DIVISION_ASSIGNED', 'IN_DELIVERY', 'REJECTED', 'WITHDRAWN', 'DELIVERED', 'CLOSED')
            ");
        }
    }
}
