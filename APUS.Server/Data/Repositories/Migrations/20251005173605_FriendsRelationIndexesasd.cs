using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class FriendsRelationIndexesasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRelation");

            migrationBuilder.CreateTable(
                name: "UserRelations",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FriendId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelations", x => new { x.UserId, x.FriendId });
                    table.CheckConstraint("CK_Friendship_NotSelf", "[UserId] <> [FriendId]");
                    table.ForeignKey(
                        name: "FK_UserRelations_AspNetUsers_FriendId",
                        column: x => x.FriendId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRelations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelations_FriendId_Status_UserId",
                table: "UserRelations",
                columns: new[] { "FriendId", "Status", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelations_UserId_Status_FriendId",
                table: "UserRelations",
                columns: new[] { "UserId", "Status", "FriendId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRelations");

            migrationBuilder.CreateTable(
                name: "UserRelation",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FriendId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelation", x => new { x.UserId, x.FriendId });
                    table.CheckConstraint("CK_Friendship_NotSelf", "[UserId] <> [FriendId]");
                    table.ForeignKey(
                        name: "FK_UserRelation_AspNetUsers_FriendId",
                        column: x => x.FriendId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRelation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_FriendId_Status_UserId",
                table: "UserRelation",
                columns: new[] { "FriendId", "Status", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_UserId_Status_FriendId",
                table: "UserRelation",
                columns: new[] { "UserId", "Status", "FriendId" });
        }
    }
}
