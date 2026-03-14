using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultilingualNamesToSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Text",
                table: "Sources");

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Sources",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Sources",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Sources");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Sources",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
