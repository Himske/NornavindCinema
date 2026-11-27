using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class MovieSeeder(ApplicationDbContext context, IWebHostEnvironment environment) {
        public async Task<byte[]> GenerateSeedAsync(CancellationToken cancellationToken = default) {
            var movies = await context.Movies.AsNoTracking().OrderBy(movie => movie.Id).ToListAsync(cancellationToken);
            var document = JsonSerializer.SerializeToNode(new MovieSeedFile { Version = 1, Movies = movies })
                ?? throw new InvalidDataException("Unable to create the movie seed file.");
            foreach (var movie in document["Movies"]!.AsArray()) movie!.AsObject().Remove("Id");
            return JsonSerializer.SerializeToUtf8Bytes(document, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default) {
            var generatedPath = Path.Combine(environment.ContentRootPath, "Data", "movie_seed.json");
            if (File.Exists(generatedPath)) return await SeedGeneratedAsync(generatedPath, cancellationToken);

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

        private async Task<SeedResult> SeedGeneratedAsync(string path, CancellationToken cancellationToken) {
            await using var stream = File.OpenRead(path);
            var seed = await JsonSerializer.DeserializeAsync<MovieSeedFile>(stream, cancellationToken: cancellationToken);
            if (seed is null || seed.Version != 1 || seed.Movies is null || seed.Movies.Any(movie => movie is null || movie.Genres is null || movie.Actors is null || movie.Ratings is null || movie.Ratings.Any(rating => rating is null))) {
                throw new InvalidDataException("The generated movie seed file is invalid or has an unsupported version.");
            }

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = await context.Movies.AsNoTracking().ToListAsync(cancellationToken);
            var imdbIds = existing.Where(movie => !string.IsNullOrWhiteSpace(movie.ImdbId)).Select(movie => movie.ImdbId!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var tmdbIds = existing.Where(movie => movie.TmdbId.HasValue).Select(movie => movie.TmdbId!.Value).ToHashSet();
            var anonymousContents = existing.Where(movie => string.IsNullOrWhiteSpace(movie.ImdbId) && !movie.TmdbId.HasValue)
                .GroupBy(MovieContent, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var added = 0;
            foreach (var movie in seed.Movies) {
                var imdbId = string.IsNullOrWhiteSpace(movie.ImdbId) ? null : movie.ImdbId.Trim();
                if ((imdbId is not null && imdbIds.Contains(imdbId)) || (movie.TmdbId.HasValue && tmdbIds.Contains(movie.TmdbId.Value))) continue;
                if (imdbId is null && !movie.TmdbId.HasValue) {
                    var content = MovieContent(movie);
                    if (anonymousContents.TryGetValue(content, out var remaining) && remaining > 0) {
                        anonymousContents[content] = remaining - 1;
                        continue;
                    }
                }
                movie.Id = 0;
                context.Movies.Add(movie);
                if (imdbId is not null) imdbIds.Add(imdbId);
                if (movie.TmdbId.HasValue) tmdbIds.Add(movie.TmdbId.Value);
                added++;
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SeedResult(added, seed.Movies.Count - added);
        }

        private static string MovieContent(Movie movie) {
            var node = JsonSerializer.SerializeToNode(movie)!.AsObject();
            node.Remove("Id");
            return node.ToJsonString();
        }

        public sealed class MovieSeedFile {
            public int Version { get; set; }
            public List<Movie>? Movies { get; set; }
        }

        public static Movie ToMovie(OmdbMovie source) {
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
            public string? Type { get; set; }
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
