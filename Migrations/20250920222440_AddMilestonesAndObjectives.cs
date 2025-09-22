using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestonesAndObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Milestones",
                newName: "TargetDate");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "MilestoneUpdates",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MilestoneUpdates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MilestoneUpdates",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "StatusChange",
                table: "MilestoneUpdates",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "MilestoneUpdates",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "UpdateText",
                table: "MilestoneUpdates",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Milestones",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "Milestones",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Milestones",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDate",
                table: "Milestones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Milestones",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "Milestones",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Milestones",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "Milestones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ObjectiveId",
                table: "Milestones",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Milestones",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ObjectiveId",
                table: "Milestones",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_Reference",
                table: "Objectives",
                column: "Reference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Milestones_Objectives_ObjectiveId",
                table: "Milestones",
                column: "ObjectiveId",
                principalTable: "Objectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Milestones_Objectives_ObjectiveId",
                table: "Milestones");

            migrationBuilder.DropTable(
                name: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_ObjectiveId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "StatusChange",
                table: "MilestoneUpdates");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "MilestoneUpdates");

            migrationBuilder.DropColumn(
                name: "UpdateText",
                table: "MilestoneUpdates");

            migrationBuilder.DropColumn(
                name: "ActualDate",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "ObjectiveId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Milestones");

            migrationBuilder.RenameColumn(
                name: "TargetDate",
                table: "Milestones",
                newName: "UpdatedBy");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "MilestoneUpdates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MilestoneUpdates",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MilestoneUpdates",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Milestones",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "Milestones",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Milestones",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);
        }
    }
}
