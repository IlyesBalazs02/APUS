using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class MoveToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Activities");

            migrationBuilder.RenameTable(
                name: "Running",
                newName: "Running",
                newSchema: "Activities");

            migrationBuilder.RenameTable(
                name: "MainActivities",
                newName: "MainActivities",
                newSchema: "Activities");

            migrationBuilder.RenameTable(
                name: "Hiking",
                newName: "Hiking",
                newSchema: "Activities");

            migrationBuilder.RenameTable(
                name: "Bouldering",
                newName: "Bouldering",
                newSchema: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Running",
                schema: "Activities",
                newName: "Running");

            migrationBuilder.RenameTable(
                name: "MainActivities",
                schema: "Activities",
                newName: "MainActivities");

            migrationBuilder.RenameTable(
                name: "Hiking",
                schema: "Activities",
                newName: "Hiking");

            migrationBuilder.RenameTable(
                name: "Bouldering",
                schema: "Activities",
                newName: "Bouldering");
        }
    }
}
