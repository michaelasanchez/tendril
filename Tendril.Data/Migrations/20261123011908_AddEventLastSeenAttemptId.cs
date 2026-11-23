using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLastSeenAttemptId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastSeenAttemptId",
                table: "Event",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_LastSeenAttemptId",
                table: "Event",
                column: "LastSeenAttemptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_ScraperAttemptHistory_LastSeenAttemptId",
                table: "Event",
                column: "LastSeenAttemptId",
                principalTable: "ScraperAttemptHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_ScraperAttemptHistory_LastSeenAttemptId",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Event_LastSeenAttemptId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "LastSeenAttemptId",
                table: "Event");
        }
    }
}
