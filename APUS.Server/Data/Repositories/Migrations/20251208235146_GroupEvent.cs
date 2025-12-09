using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class GroupEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupEvents",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TrackActivityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupEvents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupEvents_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Social",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupEvents_MainActivities_TrackActivityId",
                        column: x => x.TrackActivityId,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupEvents_CreatedByUserId",
                schema: "Social",
                table: "GroupEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEvents_GroupId_StartsAtUtc_CreatedAtUtc_Id",
                schema: "Social",
                table: "GroupEvents",
                columns: new[] { "GroupId", "StartsAtUtc", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupEvents_TrackActivityId",
                schema: "Social",
                table: "GroupEvents",
                column: "TrackActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupEvents",
                schema: "Social");
        }
    }
}
