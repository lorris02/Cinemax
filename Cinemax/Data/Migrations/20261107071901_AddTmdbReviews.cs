using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemax.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Reviews",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TmdbReviewId",
                table: "Reviews",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TmdbDetailsLoaded",
                table: "Movies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "TmdbRating",
                table: "Movies",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TmdbReviewId",
                table: "Reviews",
                column: "TmdbReviewId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_TmdbReviewId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TmdbReviewId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "TmdbDetailsLoaded",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TmdbRating",
                table: "Movies");
        }
    }
}
