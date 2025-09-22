using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAddProduct = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditProduct = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDeleteProduct = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAddMetric = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditMetric = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDeleteMetric = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAddMilestone = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditMilestone = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDeleteMilestone = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanAddUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanDeleteUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewReports = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanSubmitReports = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_Email",
                table: "UserPermissions",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPermissions");
        }
    }
}
