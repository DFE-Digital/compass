using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261005134000_AddServiceSchemaQuestionResponseMode")]
    public partial class AddServiceSchemaQuestionResponseMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogueKind",
                table: "ServiceSchemaQuestions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChoiceOptions",
                table: "ServiceSchemaQuestions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LookupSource",
                table: "ServiceSchemaQuestions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseMode",
                table: "ServiceSchemaQuestions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CatalogueKind", table: "ServiceSchemaQuestions");
            migrationBuilder.DropColumn(name: "ChoiceOptions", table: "ServiceSchemaQuestions");
            migrationBuilder.DropColumn(name: "LookupSource", table: "ServiceSchemaQuestions");
            migrationBuilder.DropColumn(name: "ResponseMode", table: "ServiceSchemaQuestions");
        }
    }
}
