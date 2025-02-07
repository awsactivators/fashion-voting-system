using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionVote.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToDesignerShow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DesignerShows",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "DesignerShows");
        }
    }
}
