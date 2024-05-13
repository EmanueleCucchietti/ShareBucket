using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedMissingFriendshipDbSetAndOtherChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_User_UserId",
                table: "Friendship");

            migrationBuilder.AddColumn<int>(
                name: "MemoryAreaId",
                table: "Metadata",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserOwnerId",
                table: "MemoryArea",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Metadata_MemoryAreaId",
                table: "Metadata",
                column: "MemoryAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryArea_UserOwnerId",
                table: "MemoryArea",
                column: "UserOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_User_UserId",
                table: "Friendship",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoryArea_User_UserOwnerId",
                table: "MemoryArea",
                column: "UserOwnerId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Metadata_MemoryArea_MemoryAreaId",
                table: "Metadata",
                column: "MemoryAreaId",
                principalTable: "MemoryArea",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_User_UserId",
                table: "Friendship");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoryArea_User_UserOwnerId",
                table: "MemoryArea");

            migrationBuilder.DropForeignKey(
                name: "FK_Metadata_MemoryArea_MemoryAreaId",
                table: "Metadata");

            migrationBuilder.DropIndex(
                name: "IX_Metadata_MemoryAreaId",
                table: "Metadata");

            migrationBuilder.DropIndex(
                name: "IX_MemoryArea_UserOwnerId",
                table: "MemoryArea");

            migrationBuilder.DropColumn(
                name: "MemoryAreaId",
                table: "Metadata");

            migrationBuilder.DropColumn(
                name: "UserOwnerId",
                table: "MemoryArea");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_User_UserId",
                table: "Friendship",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
