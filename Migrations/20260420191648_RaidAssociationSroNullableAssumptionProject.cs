using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidAssociationSroNullableAssumptionProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RaidAssociationKind",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SroUserId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaidAssociationKind",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SroUserId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Assumptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PrimaryProductId",
                table: "Assumptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaidAssociationKind",
                table: "Assumptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SroUserId",
                table: "Assumptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_SroUserId",
                table: "Risks",
                column: "SroUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_SroUserId",
                table: "Issues",
                column: "SroUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_PrimaryProductId",
                table: "Assumptions",
                column: "PrimaryProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Assumptions_SroUserId",
                table: "Assumptions",
                column: "SroUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assumptions_Service_PrimaryProductId",
                table: "Assumptions",
                column: "PrimaryProductId",
                principalTable: "Service",
                principalColumn: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assumptions_Users_SroUserId",
                table: "Assumptions",
                column: "SroUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_SroUserId",
                table: "Issues",
                column: "SroUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_SroUserId",
                table: "Risks",
                column: "SroUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assumptions_Service_PrimaryProductId",
                table: "Assumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Assumptions_Users_SroUserId",
                table: "Assumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_SroUserId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_SroUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_SroUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Issues_SroUserId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Assumptions_PrimaryProductId",
                table: "Assumptions");

            migrationBuilder.DropIndex(
                name: "IX_Assumptions_SroUserId",
                table: "Assumptions");

            migrationBuilder.DropColumn(
                name: "RaidAssociationKind",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "SroUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RaidAssociationKind",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SroUserId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "PrimaryProductId",
                table: "Assumptions");

            migrationBuilder.DropColumn(
                name: "RaidAssociationKind",
                table: "Assumptions");

            migrationBuilder.DropColumn(
                name: "SroUserId",
                table: "Assumptions");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Assumptions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
