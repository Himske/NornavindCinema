using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.Models;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    public class MoviesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller {
        public async Task<IActionResult> Index() {
            var movies = await context.Movies
                .AsNoTracking()
                .OrderBy(movie => movie.Title)
                .ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Details(int? id) {
            if (id is null) {
                return NotFound();
            }

            var model = await LoadDetailsAsync(id.Value);
            return model is null ? NotFound() : View(model);
        }

        [Authorize(Policy = "MovieMember")]
        public async Task<IActionResult> Favorites() {
            var userId = userManager.GetUserId(User);
            if (userId is null) return Challenge();

            var movies = await context.MovieFavorites.AsNoTracking()
                .Where(favorite => favorite.UserId == userId)
                .Select(favorite => favorite.Movie)
                .OrderBy(movie => movie.Title)
                .ToListAsync();
            return View(movies);
        }

        [Authorize(Policy = "MovieMember")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Favorite(int id, bool isFavorite, string? returnTo) {
            var userId = userManager.GetUserId(User);
            if (userId is null) return Challenge();
            if (!await context.Movies.AnyAsync(movie => movie.Id == id)) return NotFound();

            var favorite = await context.MovieFavorites.FindAsync(userId, id);
            if (isFavorite && favorite is null) {
                context.MovieFavorites.Add(new MovieFavorite { UserId = userId, MovieId = id });
            }
            else if (!isFavorite && favorite is not null) {
                context.MovieFavorites.Remove(favorite);
            }
            await context.SaveChangesAsync();
            return returnTo == "Favorites" ? RedirectToAction(nameof(Favorites)) : RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "MovieMember")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, [Bind(Prefix = "Feedback")] MovieReviewInput feedback) {
            var userId = userManager.GetUserId(User);
            if (userId is null) return Challenge();
            var model = await LoadDetailsAsync(id);
            if (model is null) return NotFound();

            if (!ModelState.IsValid) {
                model.Feedback = feedback;
                return View("Details", model);
            }

            var review = await context.MemberMovieReviews.FindAsync(userId, id);
            if (review is null) {
                review = new MemberMovieReview { UserId = userId, MovieId = id };
                context.MemberMovieReviews.Add(review);
            }
            review.Rating = feedback.Rating;
            review.Review = string.IsNullOrWhiteSpace(feedback.Review) ? null : feedback.Review.Trim();
            await context.SaveChangesAsync();
            TempData["ReviewSaved"] = "Your review and rating were saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<MovieDetailsViewModel?> LoadDetailsAsync(int id) {
            var movie = await context.Movies.AsNoTracking().FirstOrDefaultAsync(movie => movie.Id == id);
            if (movie is null) return null;

            var reviews = await context.MemberMovieReviews.AsNoTracking().Where(review => review.MovieId == id && !review.IsHidden)
                .OrderBy(review => review.UserId).ToListAsync();
            var userId = userManager.GetUserId(User);
            var own = userId is null ? null : await context.MemberMovieReviews.AsNoTracking()
                .FirstOrDefaultAsync(review => review.MovieId == id && review.UserId == userId);
            return new MovieDetailsViewModel {
                Movie = movie,
                Reviews = reviews,
                OwnReviewIsHidden = own?.IsHidden ?? false,
                IsFavorite = userId is not null && await context.MovieFavorites.AsNoTracking().AnyAsync(favorite => favorite.UserId == userId && favorite.MovieId == id),
                Feedback = new MovieReviewInput { Rating = own?.Rating, Review = own?.Review }
            };
        }

        [Authorize(Policy = "Administrator")]
        public IActionResult Create() => View(new MovieFormViewModel());

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel form) {
            if (!ModelState.IsValid) {
                return View(form);
            }

            var movie = new Movie();
            form.ApplyTo(movie);
            context.Movies.Add(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Edit(int? id) {
            if (id is null) {
                return NotFound();
            }

            var movie = await context.Movies.AsNoTracking().FirstOrDefaultAsync(movie => movie.Id == id);
            return movie is null ? NotFound() : View(MovieFormViewModel.FromMovie(movie));
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieFormViewModel form) {
            if (!ModelState.IsValid) {
                return View(form);
            }

            var movie = await context.Movies.FirstOrDefaultAsync(movie => movie.Id == id);
            if (movie is null) {
                return NotFound();
            }

            form.ApplyTo(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Delete(int? id) {
            if (id is null) {
                return NotFound();
            }

            var movie = await context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.Id == id);
            return movie is null ? NotFound() : View(movie);
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var movie = await context.Movies.FindAsync(id);
            if (movie is null) {
                return NotFound();
            }

            context.Movies.Remove(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
