using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class MovieSeeder(ApplicationDbContext context, IWebHostEnvironment environment) {
        public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default) {
            var path = Path.Combine(environment.ContentRootPath, "Data", "omdb_responses.json");
            await using var stream = File.OpenRead(path);
            var responses = await JsonSerializer.DeserializeAsync<List<OmdbMovie>>(stream, cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("The movie seed file does not contain a movie array.");
            if (responses.Count == 0) {
                throw new InvalidDataException("The movie seed file is empty.");
            }

            var movies = responses.Select(ToMovie).ToList();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existingIds = await context.Movies
                .Where(movie => movie.ImdbId != null)
                .Select(movie => movie.ImdbId!)
                .ToListAsync(cancellationToken);
            var knownIds = new HashSet<string>(existingIds.Select(id => id.Trim()), StringComparer.OrdinalIgnoreCase);
            var newMovies = movies.Where(movie => knownIds.Add(movie.ImdbId!)).ToList();

            context.Movies.AddRange(newMovies);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SeedResult(newMovies.Count, movies.Count - newMovies.Count);
        }

        private static Movie ToMovie(OmdbMovie source) {
            if (!string.Equals(source.Response, "True", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(source.Title)
                || string.IsNullOrWhiteSpace(source.ImdbId)
                || string.Equals(source.ImdbId.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidDataException($"Invalid OMDB record: title '{source.Title}', IMDb ID '{source.ImdbId}'.");
            }

            DateOnly? released = null;
            if (!string.IsNullOrWhiteSpace(source.Released) && !string.Equals(source.Released.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)) {
                if (!DateOnly.TryParseExact(source.Released.Trim(), "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
                    throw new InvalidDataException($"Invalid release date '{source.Released}' for '{source.Title}'.");
                }
                released = date;
            }

            return new Movie {
                Title = source.Title,
                Year = source.Year,
                Rated = source.Rated,
                Released = released,
                Runtime = source.Runtime,
                Genres = SplitList(source.Genre),
                Director = source.Director,
                Writer = source.Writer,
                Actors = SplitList(source.Actors),
                Plot = source.Plot,
                Language = source.Language,
                Country = source.Country,
                Awards = source.Awards,
                Poster = string.Equals(source.Poster?.Trim(), "N/A", StringComparison.OrdinalIgnoreCase) ? null : source.Poster,
                Ratings = source.Ratings ?? [],
                Metascore = source.Metascore,
                ImdbRating = source.ImdbRating,
                ImdbVotes = source.ImdbVotes,
                ImdbId = source.ImdbId.Trim(),
                BoxOffice = source.BoxOffice,
                Production = source.Production
            };
        }

        private static List<string> SplitList(string? value) =>
            string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
                ? []
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        public sealed record SeedResult(int Added, int Skipped);

        public sealed class OmdbMovie {
            public string? Title { get; set; }
            public string? Year { get; set; }
            public string? Rated { get; set; }
            public string? Released { get; set; }
            public string? Runtime { get; set; }
            public string? Genre { get; set; }
            public string? Director { get; set; }
            public string? Writer { get; set; }
            public string? Actors { get; set; }
            public string? Plot { get; set; }
            public string? Language { get; set; }
            public string? Country { get; set; }
            public string? Awards { get; set; }
            public string? Poster { get; set; }
            public List<MovieRating>? Ratings { get; set; }
            public string? Metascore { get; set; }
            [JsonPropertyName("imdbRating")]
            public string? ImdbRating { get; set; }
            [JsonPropertyName("imdbVotes")]
            public string? ImdbVotes { get; set; }
            [JsonPropertyName("imdbID")]
            public string? ImdbId { get; set; }
            public string? BoxOffice { get; set; }
            public string? Production { get; set; }
            public string? Response { get; set; }
        }
    }
}
