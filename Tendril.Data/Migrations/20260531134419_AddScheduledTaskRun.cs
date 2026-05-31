using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTaskRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTask_ScheduledTaskId",
                table: "ScraperAttemptHistory");

            migrationBuilder.RenameColumn(
                name: "ScheduledTaskId",
                table: "ScraperAttemptHistory",
                newName: "ScheduledTaskRunId");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperAttemptHistory_ScheduledTaskId",
                table: "ScraperAttemptHistory",
                newName: "IX_ScraperAttemptHistory_ScheduledTaskRunId");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ScheduledTask");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ScheduledTask",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "ScheduledTask",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "ScheduledTaskRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledTaskRuns_ScheduledTask_ScheduledTaskId",
                        column: x => x.ScheduledTaskId,
                        principalTable: "ScheduledTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskRuns_ScheduledTaskId",
                table: "ScheduledTaskRuns",
                column: "ScheduledTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTaskRuns_ScheduledTaskRunId",
                table: "ScraperAttemptHistory",
                column: "ScheduledTaskRunId",
                principalTable: "ScheduledTaskRuns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTaskRuns_ScheduledTaskRunId",
                table: "ScraperAttemptHistory");

            migrationBuilder.DropTable(
                name: "ScheduledTaskRuns");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "ScheduledTask");

            migrationBuilder.RenameColumn(
                name: "ScheduledTaskRunId",
                table: "ScraperAttemptHistory",
                newName: "ScheduledTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperAttemptHistory_ScheduledTaskRunId",
                table: "ScraperAttemptHistory",
                newName: "IX_ScraperAttemptHistory_ScheduledTaskId");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ScheduledTask");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ScheduledTask",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTask_ScheduledTaskId",
                table: "ScraperAttemptHistory",
                column: "ScheduledTaskId",
                principalTable: "ScheduledTask",
                principalColumn: "Id");
        }
    }
}
