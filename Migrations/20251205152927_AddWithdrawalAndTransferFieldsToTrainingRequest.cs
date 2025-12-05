using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalAndTransferFieldsToTrainingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WithdrawalRequested",
                table: "TrainingRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "TrainingRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawalRequestedAt",
                table: "TrainingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransferToUserId",
                table: "TrainingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_TransferToUserId",
                table: "TrainingRequests",
                column: "TransferToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRequests_Users_TransferToUserId",
                table: "TrainingRequests",
                column: "TransferToUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRequests_Users_TransferToUserId",
                table: "TrainingRequests");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRequests_TransferToUserId",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "WithdrawalRequested",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "WithdrawalRequestedAt",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "TransferToUserId",
                table: "TrainingRequests");
        }
    }
}
