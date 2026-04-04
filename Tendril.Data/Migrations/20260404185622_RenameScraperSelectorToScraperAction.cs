using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameScraperSelectorToScraperAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperSelector");

            migrationBuilder.DropForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ScraperDefinitionId",
                table: "ScraperSelector");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScraperSelector",
                table: "ScraperSelector");

            migrationBuilder.RenameTable(
                name: "ScraperSelector",
                newName: "ScraperAction");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperSelector_ScraperDefinitionId",
                table: "ScraperAction",
                newName: "IX_ScraperAction_ScraperDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperSelector_ChildScraperDefinitionId",
                table: "ScraperAction",
                newName: "IX_ScraperAction_ChildScraperDefinitionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScraperAction",
                table: "ScraperAction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperAction_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperAction",
                column: "ChildScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperAction_ScraperDefinition_ScraperDefinitionId",
                table: "ScraperAction",
                column: "ScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperAction_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperAction");

            migrationBuilder.DropForeignKey(
                name: "FK_ScraperAction_ScraperDefinition_ScraperDefinitionId",
                table: "ScraperAction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScraperAction",
                table: "ScraperAction");

            migrationBuilder.RenameTable(
                name: "ScraperAction",
                newName: "ScraperSelector");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperAction_ScraperDefinitionId",
                table: "ScraperSelector",
                newName: "IX_ScraperSelector_ScraperDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScraperAction_ChildScraperDefinitionId",
                table: "ScraperSelector",
                newName: "IX_ScraperSelector_ChildScraperDefinitionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScraperSelector",
                table: "ScraperSelector",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperSelector",
                column: "ChildScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ScraperDefinitionId",
                table: "ScraperSelector",
                column: "ScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
