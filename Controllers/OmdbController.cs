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
    public class OmdbController(OmdbClient omdb, ApplicationDbContext context, ILogger<OmdbController> logger) : Controller {
        public async Task<IActionResult> Index(string? query, int page = 1, CancellationToken cancellationToken = default) {
            var model = new OmdbSearchViewModel { Query = query?.Trim() ?? "", Page = page };
            if (query is null) return View(model);
            if (model.Query.Length is 0 or > 100 || page is < 1 or > 100) {
                model.Error = "Enter a search term of 1 to 100 characters and a valid page number.";
                return View(model);
            }

            try {
                var result = await omdb.SearchAsync(model.Query, page, cancellationToken);
                if (!string.Equals(result.Response, "True", StringComparison.OrdinalIgnoreCase)) {
                    model.Error = string.Equals(result.Error, "Movie not found!", StringComparison.OrdinalIgnoreCase)
                        ? "No movies found." : "OMDB could not complete the search. Check your API key and try again.";
                    return View(model);
                }
                if (result.Search is null || result.TotalResults < 1 || result.Search.Any(item => !ValidId(item.ImdbId) || string.IsNullOrWhiteSpace(item.Title) || !string.Equals(item.Type, "movie", StringComparison.OrdinalIgnoreCase))) {
                    throw new InvalidDataException("Invalid OMDB search response.");
                }
                var existing = await context.Movies.AsNoTracking().Where(movie => movie.ImdbId != null)
                    .Select(movie => movie.ImdbId!).ToListAsync(cancellationToken);
                var known = existing.Select(id => id.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var item in result.Search) item.AlreadyImported = known.Contains(item.ImdbId!);
                model.Results = result.Search;
                model.TotalResults = result.TotalResults;
            }
            catch (OmdbConfigurationException) {
                model.Error = "Configure an OMDB API key before searching.";
            }
            catch (Exception ex) when (IsOmdbFailure(ex, cancellationToken)) {
                logger.LogWarning("OMDB search failed ({ErrorType}).", ex.GetType().Name);
                model.Error = "Unable to search OMDB. Check the API configuration and try again.";
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(string? imdbId, string? query, int page = 1, CancellationToken cancellationToken = default) {
            if (!ValidId(imdbId) || query?.Length > 100 || page is < 1 or > 100) return BadRequest();
            try {
                var source = await omdb.GetMovieAsync(imdbId!, cancellationToken);
                if (!string.Equals(source.ImdbId, imdbId, StringComparison.OrdinalIgnoreCase) || !string.Equals(source.Type, "movie", StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidDataException("OMDB returned a different or non-movie record.");
                }
                var movie = MovieSeeder.ToMovie(source);
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var normalizedId = imdbId!.ToLowerInvariant();
                var exists = await context.Movies.AnyAsync(existing => existing.ImdbId != null && existing.ImdbId.Trim().ToLower() == normalizedId, cancellationToken);
                if (exists) {
                    TempData["OmdbMessage"] = "That movie is already in the database; it was not changed.";
                }
                else {
                    context.Movies.Add(movie);
                    await context.SaveChangesAsync(cancellationToken);
                    TempData["OmdbMessage"] = "Movie imported successfully.";
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (OmdbConfigurationException) {
                TempData["OmdbError"] = "Configure an OMDB API key before importing.";
            }
            catch (Exception ex) when (IsOmdbFailure(ex, cancellationToken)) {
                logger.LogWarning("OMDB import failed ({ErrorType}).", ex.GetType().Name);
                TempData["OmdbError"] = "Unable to import this movie from OMDB. Check the API configuration and try again.";
            }
            return RedirectToAction(nameof(Index), new { query, page });
        }

        private static bool ValidId(string? id) => id is not null && Regex.IsMatch(id, @"\Att[0-9]{1,15}\z", RegexOptions.CultureInvariant);

        private static bool IsOmdbFailure(Exception ex, CancellationToken cancellationToken) =>
            ex is HttpRequestException or JsonException or InvalidDataException or CryptographicException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);
    }
}
