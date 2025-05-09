using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class GPSRelatedActivityTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hiking_MainActivities_Id",
                schema: "Activities",
                table: "Hiking");

            migrationBuilder.DropForeignKey(
                name: "FK_Running_MainActivities_Id",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropColumn(
                name: "Distance",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropColumn(
                name: "ElevationGain",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropColumn(
                name: "Pace",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropColumn(
                name: "GPXPath",
                schema: "Activities",
                table: "MainActivities");

            migrationBuilder.DropColumn(
                name: "Distance",
                schema: "Activities",
                table: "Hiking");

            migrationBuilder.DropColumn(
                name: "ElevationGain",
                schema: "Activities",
                table: "Hiking");

            migrationBuilder.CreateTable(
                name: "GpsRelatedActivities",
                schema: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalDistanceKm = table.Column<double>(type: "float", nullable: true),
                    TotalAscentMeters = table.Column<double>(type: "float", nullable: true),
                    TotalDescentMeters = table.Column<double>(type: "float", nullable: true),
                    AvgPace = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsRelatedActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GpsRelatedActivities_MainActivities_Id",
                        column: x => x.Id,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Hiking_GpsRelatedActivities_Id",
                schema: "Activities",
                table: "Hiking",
                column: "Id",
                principalSchema: "Activities",
                principalTable: "GpsRelatedActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Running_GpsRelatedActivities_Id",
                schema: "Activities",
                table: "Running",
                column: "Id",
                principalSchema: "Activities",
                principalTable: "GpsRelatedActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hiking_GpsRelatedActivities_Id",
                schema: "Activities",
                table: "Hiking");

            migrationBuilder.DropForeignKey(
                name: "FK_Running_GpsRelatedActivities_Id",
                schema: "Activities",
                table: "Running");

            migrationBuilder.DropTable(
                name: "GpsRelatedActivities",
                schema: "Activities");

            migrationBuilder.AddColumn<int>(
                name: "Distance",
                schema: "Activities",
                table: "Running",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElevationGain",
                schema: "Activities",
                table: "Running",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pace",
                schema: "Activities",
                table: "Running",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GPXPath",
                schema: "Activities",
                table: "MainActivities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Distance",
                schema: "Activities",
                table: "Hiking",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElevationGain",
                schema: "Activities",
                table: "Hiking",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Hiking_MainActivities_Id",
                schema: "Activities",
                table: "Hiking",
                column: "Id",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Running_MainActivities_Id",
                schema: "Activities",
                table: "Running",
                column: "Id",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
