using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperPaginationAndSelectorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDynamic",
                table: "ScraperDefinition");

            migrationBuilder.DropColumn(
                name: "Schedule",
                table: "ScraperDefinition");

            migrationBuilder.AddColumn<Guid>(
                name: "ChildScraperDefinitionId",
                table: "ScraperSelector",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InteractionValue",
                table: "ScraperSelector",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaginationTrigger",
                table: "ScraperSelector",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaginationType",
                table: "ScraperDefinition",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildScraperDefinitionId",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "InteractionValue",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "IsPaginationTrigger",
                table: "ScraperSelector");

            migrationBuilder.DropColumn(
                name: "PaginationType",
                table: "ScraperDefinition");

            migrationBuilder.AddColumn<bool>(
                name: "IsDynamic",
                table: "ScraperDefinition",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                table: "ScraperDefinition",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
