using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Venue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScraperDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDynamic = table.Column<bool>(type: "bit", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSuccessUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailureUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperDefinition_Venue_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TicketUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Event_ScraperDefinition_ScraperDefinitionId",
                        column: x => x.ScraperDefinitionId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Event_Venue_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venue",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScrapedEventRaw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScrapedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedEventRaw", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapedEventRaw_ScraperDefinition_ScraperDefinitionId",
                        column: x => x.ScraperDefinitionId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperAttemptHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ExtractedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperAttemptHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperAttemptHistory_ScraperDefinition_ScraperDefinitionId",
                        column: x => x.ScraperDefinitionId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperMappingRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputField = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OutputField = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TransformType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransformArgsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperMappingRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperMappingRule_ScraperDefinition_ScraperDefinitionId",
                        column: x => x.ScraperDefinitionId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperSelector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScraperDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SelectorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Selector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Multiple = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSelector", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperSelector_ScraperDefinition_ScraperDefinitionId",
                        column: x => x.ScraperDefinitionId,
                        principalTable: "ScraperDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_ScraperDefinitionId",
                table: "Event",
                column: "ScraperDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_VenueId",
                table: "Event",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedEventRaw_ScraperDefinitionId",
                table: "ScrapedEventRaw",
                column: "ScraperDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperAttemptHistory_ScraperDefinitionId",
                table: "ScraperAttemptHistory",
                column: "ScraperDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperDefinition_VenueId",
                table: "ScraperDefinition",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperMappingRule_ScraperDefinitionId",
                table: "ScraperMappingRule",
                column: "ScraperDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSelector_ScraperDefinitionId",
                table: "ScraperSelector",
                column: "ScraperDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "ScrapedEventRaw");

            migrationBuilder.DropTable(
                name: "ScraperAttemptHistory");

            migrationBuilder.DropTable(
                name: "ScraperMappingRule");

            migrationBuilder.DropTable(
                name: "ScraperSelector");

            migrationBuilder.DropTable(
                name: "ScraperDefinition");

            migrationBuilder.DropTable(
                name: "Venue");
        }
    }
}
