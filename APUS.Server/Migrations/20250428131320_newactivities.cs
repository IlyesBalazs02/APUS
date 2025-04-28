using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class newactivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedPoint",
                schema: "Activities",
                table: "Bouldering");

            migrationBuilder.RenameColumn(
                name: "Elevation",
                schema: "Activities",
                table: "Hiking",
                newName: "ElevationGain");

            migrationBuilder.AddColumn<int>(
                name: "ElevationGain",
                schema: "Activities",
                table: "Running",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCoordinates",
                schema: "Activities",
                table: "MainActivities",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElevationGain",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropColumn(
                name: "ShowCoordinates",
                schema: "Activities",
                table: "MainActivities");

            migrationBuilder.RenameColumn(
                name: "ElevationGain",
                schema: "Activities",
                table: "Hiking",
                newName: "Elevation");

            migrationBuilder.AddColumn<bool>(
                name: "RedPoint",
                schema: "Activities",
                table: "Bouldering",
                type: "bit",
                nullable: true);
        }
    }
}
