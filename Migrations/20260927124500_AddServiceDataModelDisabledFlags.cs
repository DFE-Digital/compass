using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20260927124500_AddServiceDataModelDisabledFlags")]
    public partial class AddServiceDataModelDisabledFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "ServiceDataModelGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "ServiceDataModelFields",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "ServiceDataModelGroups");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "ServiceDataModelFields");
        }
    }
}
