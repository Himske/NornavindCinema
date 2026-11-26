using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace NornavindCinema.Data {
    public class OmdbClient(HttpClient client, ApiCredentialStore credentials) {
        public async Task<OmdbSearchResponse> SearchAsync(string query, int page, CancellationToken cancellationToken) {
            var response = await GetAsync<OmdbSearchResponse>(new Dictionary<string, string?> {
                ["s"] = query, ["type"] = "movie", ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }, cancellationToken);
            return response;
        }

        public async Task<MovieSeeder.OmdbMovie> GetMovieAsync(string imdbId, CancellationToken cancellationToken) =>
            await GetAsync<MovieSeeder.OmdbMovie>(new Dictionary<string, string?> { ["i"] = imdbId, ["plot"] = "full" }, cancellationToken);

        private async Task<T> GetAsync<T>(Dictionary<string, string?> parameters, CancellationToken cancellationToken) {
            var key = await credentials.GetAsync(ApiCredentialStore.OmdbApiKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(key)) throw new OmdbConfigurationException();
            parameters["apikey"] = key;
            var requestUri = QueryHelpers.AddQueryString("", parameters);
            using var response = await client.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new InvalidDataException("OMDB returned an empty response.");
        }
    }

    public class OmdbConfigurationException : Exception;

    public class OmdbSearchResponse {
        public string? Response { get; set; }
        public string? Error { get; set; }
        public int TotalResults { get; set; }
        public List<OmdbSearchItem>? Search { get; set; }
    }

    public class OmdbSearchItem {
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? Type { get; set; }
        public string? Poster { get; set; }
        [JsonPropertyName("imdbID")]
        public string? ImdbId { get; set; }
        [JsonIgnore]
        public bool AlreadyImported { get; set; }
    }
}
