using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEventIsReviewRequiredAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReviewRequiredAtUtc",
                table: "Event",
                newName: "RequiresReviewAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequiresReviewAtUtc",
                table: "Event",
                newName: "ReviewRequiredAtUtc");
        }
    }
}
