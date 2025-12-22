using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptHistoryMappedCreatedAndUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExtractedCount",
                table: "ScraperAttemptHistory",
                newName: "Updated");

            migrationBuilder.AddColumn<int>(
                name: "Created",
                table: "ScraperAttemptHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Extracted",
                table: "ScraperAttemptHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mapped",
                table: "ScraperAttemptHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "ScraperAttemptHistory");

            migrationBuilder.DropColumn(
                name: "Extracted",
                table: "ScraperAttemptHistory");

            migrationBuilder.DropColumn(
                name: "Mapped",
                table: "ScraperAttemptHistory");

            migrationBuilder.RenameColumn(
                name: "Updated",
                table: "ScraperAttemptHistory",
                newName: "ExtractedCount");
        }
    }
}
