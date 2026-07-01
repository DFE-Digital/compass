using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTokenSelfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "ApiTokens",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "ApiTokens",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectSlug",
                table: "ApiTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessTier",
                table: "ApiTokens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelfService",
                table: "ApiTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApiTokenRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProjectSlug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PermissionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsReadOnlyAllData = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IssuedApiTokenId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokenRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokenRequests_ApiTokens_IssuedApiTokenId",
                        column: x => x.IssuedApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApiTokenMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiTokenId = table.Column<int>(type: "int", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AddedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokenMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokenMembers_ApiTokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "ApiTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_Name",
                table: "ApiTokens",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_OwnerEmail",
                table: "ApiTokens",
                column: "OwnerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenMembers_ApiTokenId_UserEmail",
                table: "ApiTokenMembers",
                columns: new[] { "ApiTokenId", "UserEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenRequests_IssuedApiTokenId",
                table: "ApiTokenRequests",
                column: "IssuedApiTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenRequests_RequestorEmail",
                table: "ApiTokenRequests",
                column: "RequestorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokenRequests_Status",
                table: "ApiTokenRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApiTokenMembers");
            migrationBuilder.DropTable(name: "ApiTokenRequests");

            migrationBuilder.DropIndex(name: "IX_ApiTokens_Name", table: "ApiTokens");
            migrationBuilder.DropIndex(name: "IX_ApiTokens_OwnerEmail", table: "ApiTokens");

            migrationBuilder.DropColumn(name: "OwnerEmail", table: "ApiTokens");
            migrationBuilder.DropColumn(name: "Environment", table: "ApiTokens");
            migrationBuilder.DropColumn(name: "ProjectSlug", table: "ApiTokens");
            migrationBuilder.DropColumn(name: "AccessTier", table: "ApiTokens");
            migrationBuilder.DropColumn(name: "IsSelfService", table: "ApiTokens");
        }
    }
}
