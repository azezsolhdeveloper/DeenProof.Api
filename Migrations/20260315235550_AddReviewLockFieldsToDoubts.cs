using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewLockFieldsToDoubts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Doubts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByReviewerId",
                table: "Doubts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doubts_LockedByReviewerId",
                table: "Doubts",
                column: "LockedByReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doubts_Users_LockedByReviewerId",
                table: "Doubts",
                column: "LockedByReviewerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doubts_Users_LockedByReviewerId",
                table: "Doubts");

            migrationBuilder.DropIndex(
                name: "IX_Doubts_LockedByReviewerId",
                table: "Doubts");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Doubts");

            migrationBuilder.DropColumn(
                name: "LockedByReviewerId",
                table: "Doubts");
        }
    }
}
