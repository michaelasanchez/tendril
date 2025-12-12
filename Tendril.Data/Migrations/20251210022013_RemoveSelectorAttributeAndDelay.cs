using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSelectorAttributeAndDelay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attribute",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "Delay",
                table: "ScraperSelector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
