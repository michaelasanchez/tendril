using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tendril.Data;

#nullable disable

namespace Tendril.Data.Migrations
{
    [DbContext(typeof(TendrilDbContext))]
    [Migration("20261107000000_AddWorkerSession")]
    public partial class AddWorkerSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerSession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastHeartbeatAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StoppedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_WorkerSession", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSession_StartedAtUtc",
                table: "WorkerSession",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSession_MachineName_StartedAtUtc",
                table: "WorkerSession",
                columns: new[] { "MachineName", "StartedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkerSession");
        }
    }
}