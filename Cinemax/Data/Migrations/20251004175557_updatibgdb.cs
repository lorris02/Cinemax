using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemax.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatibgdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MovieTitle",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MovieTitle",
                table: "Reviews");
        }
    }
}
