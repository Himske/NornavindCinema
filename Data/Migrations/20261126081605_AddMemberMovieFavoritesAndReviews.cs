using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NornavindCinema.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberMovieFavoritesAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberMovieReviews",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Review = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberMovieReviews", x => new { x.UserId, x.MovieId });
                    table.CheckConstraint("CK_MemberMovieReviews_Rating", "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 10)");
                    table.ForeignKey(
                        name: "FK_MemberMovieReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberMovieReviews_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieFavorites",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MovieId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieFavorites", x => new { x.UserId, x.MovieId });
                    table.ForeignKey(
                        name: "FK_MovieFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieFavorites_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberMovieReviews_MovieId",
                table: "MemberMovieReviews",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieFavorites_MovieId",
                table: "MovieFavorites",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberMovieReviews");

            migrationBuilder.DropTable(
                name: "MovieFavorites");
        }
    }
}
