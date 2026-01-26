using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tendril.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Event",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                    UPDATE [Event]
                    SET [Status] =
                        CASE
                            WHEN Disabled = 1 THEN 'Suppressed'
                            WHEN Pending = 1 THEN 'Pending'
                            ELSE 'Published'
                        END
                ");

            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "Pending",
                table: "Event");

            migrationBuilder.RenameColumn(
                name: "DisabledAtUtc",
                table: "Event",
                newName: "StatusAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Event");

            migrationBuilder.RenameColumn(
                name: "StatusAtUtc",
                table: "Event",
                newName: "DisabledAtUtc");

            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Event",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Pending",
                table: "Event",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
