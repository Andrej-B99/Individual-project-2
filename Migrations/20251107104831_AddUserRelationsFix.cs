using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterServicePlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRelationsFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfileId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MasterId",
                table: "AspNetUsers",
                column: "MasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Masters_MasterId",
                table: "AspNetUsers",
                column: "MasterId",
                principalTable: "Masters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Masters_MasterId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_MasterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "AspNetUsers");
        }
    }
}
