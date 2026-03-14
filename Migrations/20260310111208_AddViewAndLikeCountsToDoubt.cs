using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddViewAndLikeCountsToDoubt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "Doubts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Doubts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "Doubts");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Doubts");
        }
    }
}
