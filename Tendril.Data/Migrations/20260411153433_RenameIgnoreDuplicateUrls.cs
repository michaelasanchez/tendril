using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameIgnoreDuplicateUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgnoreDuplicateUrls",
                table: "ScraperAction",
                newName: "AllowDuplicateUrls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AllowDuplicateUrls",
                table: "ScraperAction",
                newName: "IgnoreDuplicateUrls");
        }
    }
}
