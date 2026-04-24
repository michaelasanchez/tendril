using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameScraperRequireReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequireReview",
                table: "ScraperDefinition",
                newName: "IsReviewRequired");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsReviewRequired",
                table: "ScraperDefinition",
                newName: "RequireReview");
        }
    }
}
