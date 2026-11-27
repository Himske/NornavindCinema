using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    public class AdminController(MovieSeeder movieSeeder, ApplicationDbContext context) : Controller {
        public async Task<IActionResult> Index(CancellationToken cancellationToken) {
            var movieCount = await context.Movies.CountAsync(cancellationToken);
            var userCount = await context.Users.CountAsync(cancellationToken);
            var reviewCount = await context.MemberMovieReviews.CountAsync(review => !review.IsHidden && review.Review != null && review.Review.Trim() != "", cancellationToken);
            var top = await context.MemberMovieReviews.AsNoTracking()
                .Where(review => !review.IsHidden && review.Rating != null)
                .GroupBy(review => new { review.MovieId, review.Movie.Title })
                .Select(group => new { group.Key.MovieId, group.Key.Title, AverageRating = group.Average(review => (double)review.Rating!.Value), RatingCount = group.Count() })
                .OrderByDescending(movie => movie.AverageRating).ThenByDescending(movie => movie.RatingCount).ThenBy(movie => movie.MovieId)
                .FirstOrDefaultAsync(cancellationToken);
            var topRated = top is null ? null : new DashboardTopMovie(top.MovieId, top.Title, top.AverageRating, top.RatingCount);
            var movieGenres = await context.Movies.AsNoTracking().Select(movie => movie.Genres).ToListAsync(cancellationToken);
            var genres = movieGenres.SelectMany(genres => genres.Where(genre => !string.IsNullOrWhiteSpace(genre))
                    .Select(genre => genre.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
                .GroupBy(genre => genre, StringComparer.OrdinalIgnoreCase)
                .Select(group => new DashboardGenre(group.Key, group.Count()))
                .OrderByDescending(genre => genre.Count).ThenBy(genre => genre.Name, StringComparer.OrdinalIgnoreCase).Take(5).ToList();
            var recentMovies = await context.Movies.AsNoTracking().OrderByDescending(movie => movie.Id)
                .Select(movie => new DashboardMovie(movie.Id, movie.Title)).Take(5).ToListAsync(cancellationToken);
            return View(new AdminDashboardViewModel {
                MovieCount = movieCount, UserCount = userCount, ReviewCount = reviewCount,
                TopRated = topRated, TopGenres = genres, RecentMovies = recentMovies
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedMovies(CancellationToken cancellationToken) {
            var result = await movieSeeder.SeedAsync(cancellationToken);
            TempData["SeedResult"] = $"Added {result.Added} movies; skipped {result.Skipped} already present or duplicate entries.";
            return RedirectToAction(nameof(Index));
        }
    }
}
