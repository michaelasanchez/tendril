using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledTaskId",
                table: "ScraperAttemptHistory",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextRunAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SelectionStrategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastRunStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastRunCompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTaskScrapers",
                columns: table => new
                {
                    ScheduledTasksId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskScrapers", x => new { x.ScheduledTasksId, x.ScraperDefinitionsId });
                    table.ForeignKey(
                        name: "FK_ScheduledTaskScrapers_ScheduledTask_ScheduledTasksId",
                        column: x => x.ScheduledTasksId,
                        principalTable: "ScheduledTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledTaskScrapers_ScraperDefinition_ScraperDefinitionsId",
                        column: x => x.ScraperDefinitionsId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScraperAttemptHistory_ScheduledTaskId",
                table: "ScraperAttemptHistory",
                column: "ScheduledTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskScrapers_ScraperDefinitionsId",
                table: "ScheduledTaskScrapers",
                column: "ScraperDefinitionsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTask_ScheduledTaskId",
                table: "ScraperAttemptHistory",
                column: "ScheduledTaskId",
                principalTable: "ScheduledTask",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperAttemptHistory_ScheduledTask_ScheduledTaskId",
                table: "ScraperAttemptHistory");

            migrationBuilder.DropTable(
                name: "ScheduledTaskScrapers");

            migrationBuilder.DropTable(
                name: "ScheduledTask");

            migrationBuilder.DropIndex(
                name: "IX_ScraperAttemptHistory_ScheduledTaskId",
                table: "ScraperAttemptHistory");

            migrationBuilder.DropColumn(
                name: "ScheduledTaskId",
                table: "ScraperAttemptHistory");
        }
    }
}
