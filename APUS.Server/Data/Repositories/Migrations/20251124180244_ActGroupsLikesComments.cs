using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class ActGroupsLikesComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    GroupPostId = table.Column<long>(type: "bigint", nullable: true),
                    ActivityId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_GroupPosts_GroupPostId",
                        column: x => x.GroupPostId,
                        principalSchema: "Social",
                        principalTable: "GroupPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_MainActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPostLikes",
                schema: "Social",
                columns: table => new
                {
                    LikedByUsersId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LikedPostsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPostLikes", x => new { x.LikedByUsersId, x.LikedPostsId });
                    table.ForeignKey(
                        name: "FK_GroupPostLikes_AspNetUsers_LikedByUsersId",
                        column: x => x.LikedByUsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPostLikes_GroupPosts_LikedPostsId",
                        column: x => x.LikedPostsId,
                        principalSchema: "Social",
                        principalTable: "GroupPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ActivityId_CreatedAtUtc_Id",
                schema: "Social",
                table: "Comments",
                columns: new[] { "ActivityId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorUserId",
                schema: "Social",
                table: "Comments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_GroupPostId_CreatedAtUtc_Id",
                schema: "Social",
                table: "Comments",
                columns: new[] { "GroupPostId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupPostLikes_LikedPostsId",
                schema: "Social",
                table: "GroupPostLikes",
                column: "LikedPostsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "GroupPostLikes",
                schema: "Social");
        }
    }
}
