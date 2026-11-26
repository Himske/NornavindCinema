using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.Models;
using NornavindCinema.ViewModels;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFromOmdb(int id, MovieFormViewModel form, [FromServices] OmdbClient omdb,
            [FromServices] ILogger<MoviesController> logger, CancellationToken cancellationToken) {
            form.Id = id;
            if (!await context.Movies.AsNoTracking().AnyAsync(movie => movie.Id == id, cancellationToken)) return NotFound();
            if (!ModelState.IsValid) return View("Edit", form);

            var imdbId = form.ImdbId?.Trim();
            if (imdbId is null || !Regex.IsMatch(imdbId, @"\Att[0-9]{1,15}\z", RegexOptions.CultureInvariant)) {
                ModelState.AddModelError(nameof(form.ImdbId), "Enter a valid IMDb ID before updating from OMDB.");
                return View("Edit", form);
            }

            try {
                var source = await omdb.GetMovieAsync(imdbId, cancellationToken);
                if (!string.Equals(source.ImdbId?.Trim(), imdbId, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(source.Type, "movie", StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidDataException("OMDB returned a different or non-movie record.");
                }

                var updated = MovieFormViewModel.FromMovie(MovieSeeder.ToMovie(source));
                updated.Id = id;
                updated.TmdbId = form.TmdbId;
                updated.Backdrop = form.Backdrop;
                ModelState.Clear();
                ViewData["OmdbRefreshMessage"] = "OMDB information loaded. Review the changes and select Save to apply them.";
                return View("Edit", updated);
            }
            catch (OmdbConfigurationException) {
                ModelState.AddModelError(string.Empty, "Configure an OMDB API key before updating this movie.");
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidDataException or CryptographicException
                || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
                logger.LogWarning("OMDB movie refresh failed ({ErrorType}).", ex.GetType().Name);
                ModelState.AddModelError(string.Empty, "Unable to update from OMDB. Check the IMDb ID and API configuration, then try again.");
            }
            return View("Edit", form);
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFromTmdb(int id, MovieFormViewModel form, [FromServices] TmdbClient tmdb,
            [FromServices] ILogger<MoviesController> logger, CancellationToken cancellationToken) {
            form.Id = id;
            if (!await context.Movies.AsNoTracking().AnyAsync(movie => movie.Id == id, cancellationToken)) return NotFound();
            if (!ModelState.IsValid) return View("Edit", form);

            var imdbId = form.ImdbId?.Trim();
            if (form.TmdbId is null && (imdbId is null || !Regex.IsMatch(imdbId, @"\Att[0-9]{1,15}\z", RegexOptions.CultureInvariant))) {
                ModelState.AddModelError(nameof(form.ImdbId), "Enter a valid IMDb ID or a TMDB ID before updating from TMDB.");
                return View("Edit", form);
            }

            try {
                var tmdbId = form.TmdbId;
                if (tmdbId is null) {
                    var matches = (await tmdb.FindByImdbIdAsync(imdbId!, cancellationToken)).MovieResults;
                    if (matches is null || matches.Count != 1 || matches[0].Id <= 0) {
                        ModelState.AddModelError(nameof(form.ImdbId), "TMDB did not find a unique movie for this IMDb ID.");
                        return View("Edit", form);
                    }
                    tmdbId = matches[0].Id;
                }

                var source = await tmdb.GetMovieAsync(tmdbId.Value, cancellationToken);
                if (source.Id != tmdbId || (!string.IsNullOrWhiteSpace(source.ImdbId) && !string.IsNullOrWhiteSpace(imdbId)
                    && !string.Equals(source.ImdbId.Trim(), imdbId, StringComparison.OrdinalIgnoreCase))) {
                    throw new InvalidDataException("TMDB returned a different movie identity.");
                }
                var movie = TmdbClient.ToMovie(source);
                form.TmdbId = movie.TmdbId;
                form.ImdbId = movie.ImdbId ?? form.ImdbId;
                form.Title = movie.Title;
                form.Year = movie.Year ?? form.Year;
                form.Released = movie.Released ?? form.Released;
                form.Runtime = movie.Runtime ?? form.Runtime;
                if (movie.Genres.Count > 0) form.Genres = string.Join(Environment.NewLine, movie.Genres);
                form.Director = movie.Director ?? form.Director;
                form.Writer = movie.Writer ?? form.Writer;
                if (movie.Actors.Count > 0) form.Actors = string.Join(Environment.NewLine, movie.Actors);
                if (!string.IsNullOrWhiteSpace(movie.Plot)) form.Plot = movie.Plot;
                if (!string.IsNullOrWhiteSpace(movie.Language)) form.Language = movie.Language;
                if (!string.IsNullOrWhiteSpace(movie.Country)) form.Country = movie.Country;
                if (!string.IsNullOrWhiteSpace(movie.Production)) form.Production = movie.Production;
                form.BoxOffice = movie.BoxOffice ?? form.BoxOffice;
                form.Poster = movie.Poster ?? form.Poster;
                form.Backdrop = movie.Backdrop ?? form.Backdrop;
                if (movie.Ratings.Count > 0) {
                    var ratings = form.Ratings.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(line => !string.Equals(line.Split('|', 2)[0].Trim(), "The Movie Database", StringComparison.OrdinalIgnoreCase))
                        .Append($"{movie.Ratings[0].Source} | {movie.Ratings[0].Value}");
                    form.Ratings = string.Join(Environment.NewLine, ratings);
                }
                ModelState.Clear();
                ViewData["TmdbRefreshMessage"] = "TMDB information loaded. Review the changes and select Save to apply them.";
                return View("Edit", form);
            }
            catch (TmdbConfigurationException) {
                ModelState.AddModelError(string.Empty, "Configure a TMDB API read access token before updating this movie.");
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidDataException or CryptographicException or FormatException
                || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
                logger.LogWarning("TMDB movie refresh failed ({ErrorType}).", ex.GetType().Name);
                ModelState.AddModelError(string.Empty, "Unable to update from TMDB. Check the movie IDs and API configuration, then try again.");
            }
            return View("Edit", form);
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
