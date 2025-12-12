using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectorAttributeAndDelayAndMappingRuleOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Attribute",
                table: "ScraperSelector",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Delay",
                table: "ScraperSelector",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "ScraperMappingRule",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attribute",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "Delay",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "ScraperMappingRule");
        }
    }
}
