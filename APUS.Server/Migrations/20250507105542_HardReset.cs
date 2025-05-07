using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class HardReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Activities");

            migrationBuilder.CreateTable(
                name: "MainActivities",
                schema: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    Calories = table.Column<int>(type: "int", nullable: true),
                    AvgHeartRate = table.Column<int>(type: "int", nullable: true),
                    MaxHeartRate = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GPXPath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bouldering",
                schema: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bouldering", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bouldering_MainActivities_Id",
                        column: x => x.Id,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hiking",
                schema: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: true),
                    ElevationGain = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hiking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hiking_MainActivities_Id",
                        column: x => x.Id,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Running",
                schema: "Activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pace = table.Column<int>(type: "int", nullable: true),
                    Distance = table.Column<int>(type: "int", nullable: true),
                    ElevationGain = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Running", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Running_MainActivities_Id",
                        column: x => x.Id,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bouldering",
                schema: "Activities");

            migrationBuilder.DropTable(
                name: "Hiking",
                schema: "Activities");

            migrationBuilder.DropTable(
                name: "Running",
                schema: "Activities");

            migrationBuilder.DropTable(
                name: "MainActivities",
                schema: "Activities");
        }
    }
}
