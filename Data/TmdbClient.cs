using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class TmdbClient(HttpClient client, ApiCredentialStore credentials) {
        public Task<TmdbSearchResponse> SearchAsync(string query, int page, CancellationToken cancellationToken) =>
            GetAsync<TmdbSearchResponse>(QueryHelpers.AddQueryString("search/movie", new Dictionary<string, string?> {
                ["query"] = query, ["include_adult"] = "false", ["language"] = "en-US", ["page"] = page.ToString(CultureInfo.InvariantCulture)
            }), cancellationToken);

        public Task<TmdbMovie> GetMovieAsync(int id, CancellationToken cancellationToken) =>
            GetAsync<TmdbMovie>($"movie/{id.ToString(CultureInfo.InvariantCulture)}?language=en-US&append_to_response=credits", cancellationToken);

        public Task<TmdbFindResponse> FindByImdbIdAsync(string imdbId, CancellationToken cancellationToken) =>
            GetAsync<TmdbFindResponse>($"find/{Uri.EscapeDataString(imdbId)}?external_source=imdb_id", cancellationToken);

        private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken) {
            var token = await credentials.GetAsync(ApiCredentialStore.TmdbReadAccessToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(token)) throw new TmdbConfigurationException();
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("TMDB returned an empty response.");
        }

        public static string? ImageUrl(string? path, string size) {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (!Regex.IsMatch(path, @"\A/[A-Za-z0-9_-]+\.(jpg|png|webp)\z", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)) {
                throw new InvalidDataException("Invalid TMDB image path.");
            }
            return $"https://image.tmdb.org/t/p/{size}{path}";
        }

        public static Movie ToMovie(TmdbMovie source) {
            if (source.Id <= 0 || string.IsNullOrWhiteSpace(source.Title)) throw new InvalidDataException("Invalid TMDB movie.");
            DateOnly? released = null;
            if (!string.IsNullOrWhiteSpace(source.ReleaseDate)) {
                if (!DateOnly.TryParseExact(source.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
                    throw new InvalidDataException("Invalid TMDB release date.");
                }
                released = date;
            }
            var crew = source.Credits?.Crew ?? [];
            return new Movie {
                TmdbId = source.Id,
                ImdbId = string.IsNullOrWhiteSpace(source.ImdbId) ? null : source.ImdbId.Trim(),
                Title = source.Title,
                Year = released?.Year.ToString(CultureInfo.InvariantCulture),
                Released = released,
                Runtime = source.Runtime > 0 ? $"{source.Runtime} min" : null,
                Genres = (source.Genres ?? []).Where(genre => !string.IsNullOrWhiteSpace(genre.Name)).Select(genre => genre.Name!).ToList(),
                Director = JoinNames(crew.Where(person => person.Job == "Director")),
                Writer = JoinNames(crew.Where(person => person.Department == "Writing")),
                Actors = (source.Credits?.Cast ?? []).Where(person => !string.IsNullOrWhiteSpace(person.Name)).Select(person => person.Name!).Take(10).ToList(),
                Plot = source.Overview,
                Language = string.Join(", ", (source.SpokenLanguages ?? []).Select(language => language.EnglishName).Where(name => !string.IsNullOrWhiteSpace(name))),
                Country = string.Join(", ", (source.ProductionCountries ?? []).Select(country => country.Name).Where(name => !string.IsNullOrWhiteSpace(name))),
                Production = string.Join(", ", (source.ProductionCompanies ?? []).Select(company => company.Name).Where(name => !string.IsNullOrWhiteSpace(name))),
                BoxOffice = source.Revenue > 0 ? source.Revenue.ToString("C0", CultureInfo.GetCultureInfo("en-US")) : null,
                Poster = ImageUrl(source.PosterPath, "w500"),
                Backdrop = ImageUrl(source.BackdropPath, "w1280"),
                Ratings = source.VoteCount > 0 && source.VoteAverage is >= 0 and <= 10
                    ? [new MovieRating { Source = "The Movie Database", Value = source.VoteAverage.ToString("0.0", CultureInfo.InvariantCulture) + "/10" }] : []
            };
        }

        private static string? JoinNames(IEnumerable<TmdbPerson> people) {
            var names = people.Select(person => person.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
            return names.Count == 0 ? null : string.Join(", ", names);
        }
    }

    public class TmdbConfigurationException : Exception;

    public class TmdbFindResponse {
        [JsonPropertyName("movie_results")] public List<TmdbMovie>? MovieResults { get; set; }
    }

    public class TmdbSearchResponse {
        public int Page { get; set; }
        [JsonPropertyName("total_pages")] public int TotalPages { get; set; }
        [JsonPropertyName("total_results")] public int TotalResults { get; set; }
        public List<TmdbMovie>? Results { get; set; }
    }

    public class TmdbMovie {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("imdb_id")] public string? ImdbId { get; set; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")] public string? BackdropPath { get; set; }
        public int? Runtime { get; set; }
        public string? Overview { get; set; }
        public long Revenue { get; set; }
        [JsonPropertyName("vote_average")] public double VoteAverage { get; set; }
        [JsonPropertyName("vote_count")] public int VoteCount { get; set; }
        public List<TmdbNamedItem>? Genres { get; set; }
        [JsonPropertyName("spoken_languages")] public List<TmdbNamedItem>? SpokenLanguages { get; set; }
        [JsonPropertyName("production_countries")] public List<TmdbNamedItem>? ProductionCountries { get; set; }
        [JsonPropertyName("production_companies")] public List<TmdbNamedItem>? ProductionCompanies { get; set; }
        public TmdbCredits? Credits { get; set; }
        [JsonIgnore] public bool AlreadyImported { get; set; }
    }
    public class TmdbCredits {
        public List<TmdbPerson>? Cast { get; set; }
        public List<TmdbPerson>? Crew { get; set; }
    }
    public class TmdbPerson {
        public string? Name { get; set; }
        public string? Job { get; set; }
        public string? Department { get; set; }
    }
    public class TmdbNamedItem {
        public string? Name { get; set; }
        [JsonPropertyName("english_name")] public string? EnglishName { get; set; }
    }
}
