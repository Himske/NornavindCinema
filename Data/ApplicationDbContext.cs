using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();
        public DbSet<MovieFavorite> MovieFavorites => Set<MovieFavorite>();
        public DbSet<MemberMovieReview> MemberMovieReviews => Set<MemberMovieReview>();

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.Entity<Movie>()
                .OwnsMany(movie => movie.Ratings, rating => {
                    rating.ToTable("MovieRatings");
                    rating.WithOwner().HasForeignKey("MovieId");
                    rating.Property<int>("Id");
                    rating.HasKey("Id");
                });

            builder.Entity<MovieFavorite>().HasKey(favorite => new { favorite.UserId, favorite.MovieId });
            builder.Entity<MovieFavorite>().HasOne(favorite => favorite.User).WithMany().HasForeignKey(favorite => favorite.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<MovieFavorite>().HasOne(favorite => favorite.Movie).WithMany().HasForeignKey(favorite => favorite.MovieId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MemberMovieReview>().HasKey(review => new { review.UserId, review.MovieId });
            builder.Entity<MemberMovieReview>().HasOne(review => review.User).WithMany().HasForeignKey(review => review.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<MemberMovieReview>().HasOne(review => review.Movie).WithMany().HasForeignKey(review => review.MovieId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<MemberMovieReview>().ToTable(table => table.HasCheckConstraint("CK_MemberMovieReviews_Rating", "[Rating] IS NULL OR ([Rating] >= 1 AND [Rating] <= 10)"));
        }
    }
}
