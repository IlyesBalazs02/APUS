using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class GroupsInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Social");

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupJoinRequests",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    RequesterUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupJoinRequests_AspNetUsers_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupJoinRequests_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Social",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMemberships",
                schema: "Social",
                columns: table => new
                {
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberships", x => new { x.GroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Social",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupJoinRequests_GroupId_RequesterUserId",
                schema: "Social",
                table: "GroupJoinRequests",
                columns: new[] { "GroupId", "RequesterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupJoinRequests_RequesterUserId",
                schema: "Social",
                table: "GroupJoinRequests",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_GroupId_Role",
                schema: "Social",
                table: "GroupMemberships",
                columns: new[] { "GroupId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_UserId",
                schema: "Social",
                table: "GroupMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatedByUserId",
                schema: "Social",
                table: "Groups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                schema: "Social",
                table: "Groups",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupJoinRequests",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "GroupMemberships",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "Social");
        }
    }
}
