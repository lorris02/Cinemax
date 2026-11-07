using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemax.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbMovieDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackdropPath",
                table: "Movies",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Overview",
                table: "Movies",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Movies",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Movies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopCast",
                table: "Movies",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "BackdropPath",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Overview",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TopCast",
                table: "Movies");
        }
    }
}
