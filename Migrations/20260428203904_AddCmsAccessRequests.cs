using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsAccessRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CmsName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SignInPageUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestorEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestorFirstName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestorLastName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DateRequested = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PublisherAccessRequired = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActionedByUserId = table.Column<int>(type: "int", nullable: true),
                    RegistrationToken = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CmsAccessRequests_Users_ActionedByUserId",
                        column: x => x.ActionedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsAccessRequests_ActionedByUserId",
                table: "CmsAccessRequests",
                column: "ActionedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmsAccessRequests");
        }
    }
}
