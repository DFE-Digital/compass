using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPreferencesToUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DashboardFocus",
                table: "UserPreferences",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredTaskGrouping",
                table: "UserPreferences",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "priority");

            migrationBuilder.AddColumn<string>(
                name: "QuickLaunchShortcuts",
                table: "UserPreferences",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMilestonePanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowProductPanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowRemindersPanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowRiskPanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSuccessPanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTasksPanel",
                table: "UserPreferences",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DashboardFocus",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredTaskGrouping",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "QuickLaunchShortcuts",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowMilestonePanel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowProductPanel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowRemindersPanel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowRiskPanel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowSuccessPanel",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowTasksPanel",
                table: "UserPreferences");
        }
    }
}
