using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class PagingBooster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_MainActivities_Date_Id",
                schema: "Activities",
                table: "MainActivities",
                columns: new[] { "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MainActivities_User_Date_Id",
                schema: "Activities",
                table: "MainActivities",
                columns: new[] { "UserId", "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteUsers_Last_First_Id",
                table: "AspNetUsers",
                columns: new[] { "LastName", "FirstName", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MainActivities_Date_Id",
                schema: "Activities",
                table: "MainActivities");

            migrationBuilder.DropIndex(
                name: "IX_MainActivities_User_Date_Id",
                schema: "Activities",
                table: "MainActivities");

            migrationBuilder.DropIndex(
                name: "IX_SiteUsers_Last_First_Id",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
