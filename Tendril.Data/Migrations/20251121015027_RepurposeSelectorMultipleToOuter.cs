using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepurposeSelectorMultipleToOuter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Multiple",
                table: "ScraperSelector",
                newName: "Outer");

            migrationBuilder.RenameColumn(
                name: "OutputField",
                table: "ScraperMappingRule",
                newName: "TargetField");

            migrationBuilder.RenameColumn(
                name: "InputField",
                table: "ScraperMappingRule",
                newName: "SourceField");

            migrationBuilder.AddColumn<string>(
                name: "CombineWithField",
                table: "ScraperMappingRule",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombineWithField",
                table: "ScraperMappingRule");

            migrationBuilder.RenameColumn(
                name: "Outer",
                table: "ScraperSelector",
                newName: "Multiple");

            migrationBuilder.RenameColumn(
                name: "TargetField",
                table: "ScraperMappingRule",
                newName: "OutputField");

            migrationBuilder.RenameColumn(
                name: "SourceField",
                table: "ScraperMappingRule",
                newName: "InputField");
        }
    }
}
