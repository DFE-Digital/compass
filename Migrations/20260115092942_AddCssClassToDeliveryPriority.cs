using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCssClassToDeliveryPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CssClass",
                table: "DeliveryPriorities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Update existing delivery priorities with CSS classes
            migrationBuilder.Sql(@"
                UPDATE DeliveryPriorities 
                SET CssClass = 'badge badge-high' 
                WHERE Name = 'High';
                
                UPDATE DeliveryPriorities 
                SET CssClass = 'badge badge-medium' 
                WHERE Name = 'Medium';
                
                UPDATE DeliveryPriorities 
                SET CssClass = 'badge badge-low' 
                WHERE Name = 'Low';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CssClass",
                table: "DeliveryPriorities");
        }
    }
}
