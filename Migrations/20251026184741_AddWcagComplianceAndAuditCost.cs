using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddWcagComplianceAndAuditCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StatementInstalled",
                table: "ProductAccessibilities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StatementUrl",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ProductAccessibilities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WcagLevel",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WcagVersion",
                table: "ProductAccessibilities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "AuditHistories",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatementInstalled",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "StatementUrl",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "WcagLevel",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "WcagVersion",
                table: "ProductAccessibilities");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "AuditHistories");
        }
    }
}
