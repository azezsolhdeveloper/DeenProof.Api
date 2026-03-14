using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerToDoubt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewerId",
                table: "Doubts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doubts_ReviewerId",
                table: "Doubts",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doubts_Users_ReviewerId",
                table: "Doubts",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doubts_Users_ReviewerId",
                table: "Doubts");

            migrationBuilder.DropIndex(
                name: "IX_Doubts_ReviewerId",
                table: "Doubts");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "Doubts");
        }
    }
}
