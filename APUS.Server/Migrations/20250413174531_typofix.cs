using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class typofix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Calpories",
                schema: "Activities",
                table: "MainActivities",
                newName: "Calories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Calories",
                schema: "Activities",
                table: "MainActivities",
                newName: "Calpories");
        }
    }
}
