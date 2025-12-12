using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class FriendsRelationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRelation_FriendId",
                table: "UserRelation");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_FriendId_Status_UserId",
                table: "UserRelation",
                columns: new[] { "FriendId", "Status", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_UserId_Status_FriendId",
                table: "UserRelation",
                columns: new[] { "UserId", "Status", "FriendId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRelation_FriendId_Status_UserId",
                table: "UserRelation");

            migrationBuilder.DropIndex(
                name: "IX_UserRelation_UserId_Status_FriendId",
                table: "UserRelation");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_FriendId",
                table: "UserRelation",
                column: "FriendId");
        }
    }
}
