using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSpanColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                schema: "Activities",
                table: "MainActivities",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "Activities",
                table: "MainActivities");
        }
    }
}
