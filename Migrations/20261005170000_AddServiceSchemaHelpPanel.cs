using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261005170000_AddServiceSchemaHelpPanel")]
    public partial class AddServiceSchemaHelpPanel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HelpPanel",
                table: "ServiceSchemaAreaConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpPanel",
                table: "ServiceSchemaQuestions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpPanel",
                table: "ServiceSchemaAreaConfigs");

            migrationBuilder.DropColumn(
                name: "HelpPanel",
                table: "ServiceSchemaQuestions");
        }
    }
}
