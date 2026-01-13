using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRevisionAttemptHistoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttemptHistoryId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EventRevision_AttemptHistoryId",
                table: "EventRevision",
                column: "AttemptHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRevision_ScraperAttemptHistory_AttemptHistoryId",
                table: "EventRevision",
                column: "AttemptHistoryId",
                principalTable: "ScraperAttemptHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRevision_ScraperAttemptHistory_AttemptHistoryId",
                table: "EventRevision");

            migrationBuilder.DropIndex(
                name: "IX_EventRevision_AttemptHistoryId",
                table: "EventRevision");

            migrationBuilder.DropColumn(
                name: "AttemptHistoryId",
                table: "EventRevision");
        }
    }
}
