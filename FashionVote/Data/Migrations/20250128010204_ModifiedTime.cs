using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionVote.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Shows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Shows",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
