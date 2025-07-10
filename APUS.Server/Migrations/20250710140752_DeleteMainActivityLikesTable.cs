using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class DeleteMainActivityLikesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "MainActivitySiteUser",
                columns: table => new
                {
                    LikedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LikedPostsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainActivitySiteUser", x => new { x.LikedById, x.LikedPostsId }).Annotation("SqlServer:Clustered", false);  // Add this
					;
					table.ForeignKey(
                        name: "FK_MainActivitySiteUser_AspNetUsers_LikedById",
                        column: x => x.LikedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MainActivitySiteUser_MainActivities_LikedPostsId",
                        column: x => x.LikedPostsId,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
	onDelete: ReferentialAction.Restrict); // <--- Change CASCADE to RESTRICT or NOACTION
				});

            migrationBuilder.CreateIndex(
                name: "IX_MainActivitySiteUser_LikedPostsId",
                table: "MainActivitySiteUser",
                column: "LikedPostsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MainActivitySiteUser");

            migrationBuilder.CreateTable(
                name: "MainActivityLikes",
                schema: "Activities",
                columns: table => new
                {
                    MainActivityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainActivityLikes", x => new { x.MainActivityId, x.UserId })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_MainActivityLikes_Activities_ActivityId",
                        column: x => x.MainActivityId,
                        principalSchema: "Activities",
                        principalTable: "MainActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MainActivityLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MainActivityLikes_UserId",
                schema: "Activities",
                table: "MainActivityLikes",
                column: "UserId");
        }
    }
}
