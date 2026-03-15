using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectorIgnoreDuplciateUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreDuplicateUrls",
                table: "ScraperSelector",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "UseYearTracking",
                table: "ScraperDefinition",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSelector_ChildScraperDefinitionId",
                table: "ScraperSelector",
                column: "ChildScraperDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperSelector",
                column: "ChildScraperDefinitionId",
                principalTable: "ScraperDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperSelector_ScraperDefinition_ChildScraperDefinitionId",
                table: "ScraperSelector");

            migrationBuilder.DropIndex(
                name: "IX_ScraperSelector_ChildScraperDefinitionId",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "IgnoreDuplicateUrls",
                table: "ScraperSelector");

            migrationBuilder.AlterColumn<bool>(
                name: "UseYearTracking",
                table: "ScraperDefinition",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
