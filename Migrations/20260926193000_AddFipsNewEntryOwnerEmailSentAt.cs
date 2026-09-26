using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260926193000_AddFipsNewEntryOwnerEmailSentAt")]
    public partial class AddFipsNewEntryOwnerEmailSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NewOwnerCompletionEmailSentAt",
                table: "CMDBProducts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE CMDBProducts
                SET NewOwnerCompletionEmailSentAt = CreatedAt
                WHERE NewOwnerCompletionEmailSentAt IS NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewOwnerCompletionEmailSentAt",
                table: "CMDBProducts");
        }
    }
}
