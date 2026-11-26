using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class TmdbController(TmdbClient tmdb, ApplicationDbContext context, ILogger<TmdbController> logger) : Controller {
        public async Task<IActionResult> Index(string? query, int page = 1, CancellationToken cancellationToken = default) {
            var model = new TmdbSearchViewModel { Query = query?.Trim() ?? "", Page = page };
            if (query is null) return View(model);
            if (model.Query.Length is 0 or > 100 || page is < 1 or > 500) {
                model.Error = "Enter a search term of 1 to 100 characters and a valid page number.";
                return View(model);
            }
            try {
                var result = await tmdb.SearchAsync(model.Query, page, cancellationToken);
                if (result.Results is null || result.Page != page || result.TotalPages < 0 || result.TotalResults < 0
                    || result.Results.Any(item => item.Id <= 0 || string.IsNullOrWhiteSpace(item.Title))) {
                    throw new InvalidDataException("Invalid TMDB search response.");
                }
                foreach (var item in result.Results) {
                    item.PosterPath = TmdbClient.ImageUrl(item.PosterPath, "w500");
                }
                var ids = result.Results.Select(item => item.Id).ToArray();
                var existing = await context.Movies.AsNoTracking().Where(movie => movie.TmdbId != null && ids.Contains(movie.TmdbId.Value))
                    .Select(movie => movie.TmdbId!.Value).ToListAsync(cancellationToken);
                var known = existing.ToHashSet();
                foreach (var item in result.Results) item.AlreadyImported = known.Contains(item.Id);
                model.Results = result.Results;
                model.TotalPages = result.TotalPages;
                model.TotalResults = result.TotalResults;
                if (result.Results.Count == 0) model.Error = "No movies found on this page.";
            }
            catch (TmdbConfigurationException) {
                model.Error = "Configure a TMDB API read access token before searching.";
            }
            catch (Exception ex) when (IsTmdbFailure(ex, cancellationToken)) {
                logger.LogWarning("TMDB search failed ({ErrorType}).", ex.GetType().Name);
                model.Error = "Unable to search TMDB. Check the API configuration and try again.";
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int id, string? query, int page = 1, CancellationToken cancellationToken = default) {
            if (id <= 0 || query?.Length > 100 || page is < 1 or > 500) return BadRequest();
            try {
                var source = await tmdb.GetMovieAsync(id, cancellationToken);
                if (source.Id != id || (!string.IsNullOrWhiteSpace(source.ImdbId) && !Regex.IsMatch(source.ImdbId.Trim(), @"\Att[0-9]{1,15}\z", RegexOptions.CultureInvariant))) {
                    throw new InvalidDataException("TMDB returned an invalid movie identity.");
                }
                var movie = TmdbClient.ToMovie(source);
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var imdbId = movie.ImdbId?.ToLowerInvariant();
                var exists = await context.Movies.AnyAsync(existing => existing.TmdbId == id ||
                    (imdbId != null && existing.ImdbId != null && existing.ImdbId.Trim().ToLower() == imdbId), cancellationToken);
                if (exists) {
                    TempData["TmdbMessage"] = "That movie is already in the database; it was not changed.";
                }
                else {
                    context.Movies.Add(movie);
                    await context.SaveChangesAsync(cancellationToken);
                    TempData["TmdbMessage"] = "Movie imported successfully.";
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (TmdbConfigurationException) {
                TempData["TmdbError"] = "Configure a TMDB API read access token before importing.";
            }
            catch (Exception ex) when (IsTmdbFailure(ex, cancellationToken)) {
                logger.LogWarning("TMDB import failed ({ErrorType}).", ex.GetType().Name);
                TempData["TmdbError"] = "Unable to import this movie from TMDB. Check the API configuration and try again.";
            }
            return RedirectToAction(nameof(Index), new { query, page });
        }

        private static bool IsTmdbFailure(Exception ex, CancellationToken cancellationToken) =>
            ex is HttpRequestException or JsonException or InvalidDataException or CryptographicException or FormatException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    }
}
