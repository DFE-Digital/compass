using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLineSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ServiceLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [ServiceLines]
                SET [Slug] = 'sl' + LOWER(REPLACE(CAST([Id] AS char(36)), '-', ''))
                WHERE [Slug] IS NULL
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "ServiceLines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLines_Slug",
                table: "ServiceLines",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceLines_Slug",
                table: "ServiceLines");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ServiceLines");
        }
    }
}
