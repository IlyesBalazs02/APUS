using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "Activities",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Distance",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pace",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RedPoint",
                table: "Activities",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Pace",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "RedPoint",
                table: "Activities");
        }
    }
}
