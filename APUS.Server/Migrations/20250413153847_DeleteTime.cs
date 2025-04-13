using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class DeleteTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Activities",
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

            migrationBuilder.RenameTable(
                name: "Activities",
                newName: "MainActivities");

            migrationBuilder.RenameColumn(
                name: "Time",
                table: "MainActivities",
                newName: "MaxHeartRate");

            migrationBuilder.RenameColumn(
                name: "HeartRate",
                table: "MainActivities",
                newName: "Calpories");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "MainActivities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<int>(
                name: "AvgHeartRate",
                table: "MainActivities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MainActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MainActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MainActivities",
                table: "MainActivities",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Bouldering",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    RedPoint = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bouldering", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bouldering_MainActivities_Id",
                        column: x => x.Id,
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hiking",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: false),
                    Elevation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hiking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hiking_MainActivities_Id",
                        column: x => x.Id,
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Running",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pace = table.Column<int>(type: "int", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Running", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Running_MainActivities_Id",
                        column: x => x.Id,
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bouldering");

            migrationBuilder.DropTable(
                name: "Hiking");

            migrationBuilder.DropTable(
                name: "Running");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MainActivities",
                table: "MainActivities");

            migrationBuilder.DropColumn(
                name: "AvgHeartRate",
                table: "MainActivities");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MainActivities");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MainActivities");

            migrationBuilder.RenameTable(
                name: "MainActivities",
                newName: "Activities");

            migrationBuilder.RenameColumn(
                name: "MaxHeartRate",
                table: "Activities",
                newName: "Time");

            migrationBuilder.RenameColumn(
                name: "Calpories",
                table: "Activities",
                newName: "HeartRate");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "Activities",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activities",
                table: "Activities",
                column: "Id");
        }
    }
}
