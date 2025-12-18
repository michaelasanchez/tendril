using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapedRawEventForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrapedEventRaw_ScraperDefinition_ScraperDefinitionId",
                table: "ScrapedEventRaw");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScraperDefinitionId",
                table: "ScrapedEventRaw",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "ScrapedEventRaw",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperAttemptHistoryId",
                table: "ScrapedEventRaw",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedEventRaw_EventId",
                table: "ScrapedEventRaw",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedEventRaw_ScraperAttemptHistoryId",
                table: "ScrapedEventRaw",
                column: "ScraperAttemptHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapedEventRaw_Event_EventId",
                table: "ScrapedEventRaw",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapedEventRaw_ScraperAttemptHistory_ScraperAttemptHistoryId",
                table: "ScrapedEventRaw",
                column: "ScraperAttemptHistoryId",
                principalTable: "ScraperAttemptHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapedEventRaw_ScraperDefinition_ScraperDefinitionId",
                table: "ScrapedEventRaw",
                column: "ScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrapedEventRaw_Event_EventId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrapedEventRaw_ScraperAttemptHistory_ScraperAttemptHistoryId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrapedEventRaw_ScraperDefinition_ScraperDefinitionId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropIndex(
                name: "IX_ScrapedEventRaw_EventId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropIndex(
                name: "IX_ScrapedEventRaw_ScraperAttemptHistoryId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "ScrapedEventRaw");

            migrationBuilder.DropColumn(
                name: "ScraperAttemptHistoryId",
                table: "ScrapedEventRaw");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScraperDefinitionId",
                table: "ScrapedEventRaw",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapedEventRaw_ScraperDefinition_ScraperDefinitionId",
                table: "ScrapedEventRaw",
                column: "ScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
