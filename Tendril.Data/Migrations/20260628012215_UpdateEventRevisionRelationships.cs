using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventRevisionRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RawEventId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "AttemptHistoryId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRevision_RelatedId",
                table: "EventRevision",
                column: "RelatedId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRevision_Event_RelatedId",
                table: "EventRevision",
                column: "RelatedId",
                principalTable: "Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRevision_Event_RelatedId",
                table: "EventRevision");

            migrationBuilder.DropIndex(
                name: "IX_EventRevision_RelatedId",
                table: "EventRevision");

            migrationBuilder.DropColumn(
                name: "RelatedId",
                table: "EventRevision");

            migrationBuilder.AlterColumn<Guid>(
                name: "RawEventId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AttemptHistoryId",
                table: "EventRevision",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
