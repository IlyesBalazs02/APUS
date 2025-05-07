using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APUS.Server.Migrations
{
    /// <inheritdoc />
    public partial class collections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityImage_MainActivities_MainActivityId",
                table: "ActivityImage");

            migrationBuilder.DropForeignKey(
                name: "FK_Coordinate_MainActivities_MainActivityId",
                table: "Coordinate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Coordinate",
                table: "Coordinate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityImage",
                table: "ActivityImage");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ActivityImage");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ActivityImage");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "ActivityImage");

            migrationBuilder.RenameTable(
                name: "Coordinate",
                newName: "Coordinates");

            migrationBuilder.RenameTable(
                name: "ActivityImage",
                newName: "ActivityImages");

            migrationBuilder.RenameIndex(
                name: "IX_Coordinate_MainActivityId",
                table: "Coordinates",
                newName: "IX_Coordinates_MainActivityId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityImage_MainActivityId",
                table: "ActivityImages",
                newName: "IX_ActivityImages_MainActivityId");

            migrationBuilder.AlterColumn<string>(
                name: "MainActivityId",
                table: "Coordinates",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainActivityId1",
                table: "Coordinates",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "MainActivityId",
                table: "ActivityImages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ActivityImages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "ActivityImages",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Coordinates",
                table: "Coordinates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityImages",
                table: "ActivityImages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Coordinates_MainActivityId1",
                table: "Coordinates",
                column: "MainActivityId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityImages_MainActivities_MainActivityId",
                table: "ActivityImages",
                column: "MainActivityId",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Coordinates_MainActivities_MainActivityId",
                table: "Coordinates",
                column: "MainActivityId",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Coordinates_MainActivities_MainActivityId1",
                table: "Coordinates",
                column: "MainActivityId1",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityImages_MainActivities_MainActivityId",
                table: "ActivityImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Coordinates_MainActivities_MainActivityId",
                table: "Coordinates");

            migrationBuilder.DropForeignKey(
                name: "FK_Coordinates_MainActivities_MainActivityId1",
                table: "Coordinates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Coordinates",
                table: "Coordinates");

            migrationBuilder.DropIndex(
                name: "IX_Coordinates_MainActivityId1",
                table: "Coordinates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityImages",
                table: "ActivityImages");

            migrationBuilder.DropColumn(
                name: "MainActivityId1",
                table: "Coordinates");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ActivityImages");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "ActivityImages");

            migrationBuilder.RenameTable(
                name: "Coordinates",
                newName: "Coordinate");

            migrationBuilder.RenameTable(
                name: "ActivityImages",
                newName: "ActivityImage");

            migrationBuilder.RenameIndex(
                name: "IX_Coordinates_MainActivityId",
                table: "Coordinate",
                newName: "IX_Coordinate_MainActivityId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityImages_MainActivityId",
                table: "ActivityImage",
                newName: "IX_ActivityImage_MainActivityId");

            migrationBuilder.AlterColumn<string>(
                name: "MainActivityId",
                table: "Coordinate",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MainActivityId",
                table: "ActivityImage",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ActivityImage",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ActivityImage",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "ActivityImage",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Coordinate",
                table: "Coordinate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityImage",
                table: "ActivityImage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityImage_MainActivities_MainActivityId",
                table: "ActivityImage",
                column: "MainActivityId",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Coordinate_MainActivities_MainActivityId",
                table: "Coordinate",
                column: "MainActivityId",
                principalSchema: "Activities",
                principalTable: "MainActivities",
                principalColumn: "Id");
        }
    }
}
