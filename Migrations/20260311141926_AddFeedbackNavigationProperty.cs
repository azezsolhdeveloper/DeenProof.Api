using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DoubtId",
                table: "Feedbacks",
                column: "DoubtId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Doubts_DoubtId",
                table: "Feedbacks",
                column: "DoubtId",
                principalTable: "Doubts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Doubts_DoubtId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_DoubtId",
                table: "Feedbacks");
        }
    }
}
