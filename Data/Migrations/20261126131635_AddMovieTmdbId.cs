using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NornavindCinema.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieTmdbId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Movies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Movies");
        }
    }
}
