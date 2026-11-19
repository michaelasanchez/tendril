using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttemptHistorySuccessToStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ScraperAttemptHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE [ScraperAttemptHistory]
                SET [Status] = CASE
                    WHEN [ErrorMessage] = N'Running...' THEN N'Running'
                    WHEN [Extracted] = 0 THEN N'Warning'
                    WHEN [Success] = 1 THEN N'Succeeded'
                    ELSE N'Failed'
                END;");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "ScraperAttemptHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ScraperAttemptHistory");

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "ScraperAttemptHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
