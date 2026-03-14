using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeenProof.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalCommentsAndSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Comments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Comments");
        }
    }
}
