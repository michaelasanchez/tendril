using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyScraperSelector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Outer",
                table: "ScraperSelector");

            migrationBuilder.RenameColumn(
                name: "SelectorType",
                table: "ScraperSelector",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "ReturnMode",
                table: "ScraperSelector",
                newName: "Order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ScraperSelector",
                newName: "SelectorType");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "ScraperSelector",
                newName: "ReturnMode");

            migrationBuilder.AddColumn<bool>(
                name: "Outer",
                table: "ScraperSelector",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
