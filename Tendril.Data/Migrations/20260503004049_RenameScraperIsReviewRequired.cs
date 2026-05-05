using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameScraperIsReviewRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsReviewRequired",
                table: "ScraperDefinition",
                newName: "RequiresReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequiresReview",
                table: "ScraperDefinition",
                newName: "IsReviewRequired");
        }
    }
}
