using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEventIsReviewRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsReviewRequired",
                table: "Event",
                newName: "RequiresReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequiresReview",
                table: "Event",
                newName: "IsReviewRequired");
        }
    }
}
