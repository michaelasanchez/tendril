using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptLogAndRawOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "ScrapedEventRaw",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Outcome",
                table: "ScrapedEventRaw",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ScraperAttemptLogEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperAttemptLogEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperAttemptLogEntry_ScraperAttemptHistory_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ScraperAttemptHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScraperAttemptLogEntry_AttemptId",
                table: "ScraperAttemptLogEntry",
                column: "AttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScraperAttemptLogEntry");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "ScrapedEventRaw");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ScrapedEventRaw");
        }
    }
}
