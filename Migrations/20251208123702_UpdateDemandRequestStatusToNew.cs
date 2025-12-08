using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDemandRequestStatusToNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE DemandRequests SET Status = 'New' WHERE Id = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Cannot reverse this update without knowing the original status value
            // If rollback is needed, the original status should be restored manually
        }
    }
}
