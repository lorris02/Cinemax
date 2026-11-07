using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinemax.Data.Migrations
{
    // Email confirmation became required after these accounts were made, so they count as confirmed.
    // This runs once; accounts made later still confirm by email.
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261108000000_ConfirmExistingAccounts")]
    public partial class ConfirmExistingAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
