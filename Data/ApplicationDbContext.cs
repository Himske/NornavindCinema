using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options) {
        public DbSet<Movie> Movies => Set<Movie>();

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.Entity<Movie>()
                .OwnsMany(movie => movie.Ratings, rating => {
                    rating.ToTable("MovieRatings");
                    rating.WithOwner().HasForeignKey("MovieId");
                    rating.Property<int>("Id");
                    rating.HasKey("Id");
                });
        }
    }
}
