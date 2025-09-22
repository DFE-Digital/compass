using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCanDeleteUserPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanDeleteUser",
                table: "UserPermissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanDeleteUser",
                table: "UserPermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
