using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingMappingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransformArgsJson",
                table: "ScraperMappingRule");

            migrationBuilder.AddColumn<string>(
                name: "RegexPattern",
                table: "ScraperMappingRule",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegexReplacement",
                table: "ScraperMappingRule",
                type: "nvarchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitDelimiter",
                table: "ScraperMappingRule",
                type: "nvarchar(16)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegexPattern",
                table: "ScraperMappingRule");

            migrationBuilder.DropColumn(
                name: "RegexReplacement",
                table: "ScraperMappingRule");

            migrationBuilder.DropColumn(
                name: "SplitDelimiter",
                table: "ScraperMappingRule");

            migrationBuilder.AddColumn<string>(
                name: "TransformArgsJson",
                table: "ScraperMappingRule",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
