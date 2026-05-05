using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperHasSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSuggestions",
                table: "ScraperDefinition",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasSuggestions",
                table: "ScraperDefinition");
        }
    }
}
