using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterServicePlatform.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewLastEditedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastEditedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastEditedAt",
                table: "Reviews");
        }
    }
}
