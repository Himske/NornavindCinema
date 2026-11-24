using System.ComponentModel.DataAnnotations;
using NornavindCinema.Models;

namespace NornavindCinema.ViewModels {
    public class MovieFormViewModel : IValidatableObject {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Year { get; set; }
        public string? Rated { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly? Released { get; set; }

        public string? Runtime { get; set; }
        public string Genres { get; set; } = string.Empty;
        public string? Director { get; set; }
        public string? Writer { get; set; }
        public string Actors { get; set; } = string.Empty;
        public string? Plot { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public string? Awards { get; set; }
        public string? Poster { get; set; }
        public string Ratings { get; set; } = string.Empty;
        public string? Metascore { get; set; }
        public string? ImdbRating { get; set; }
        public string? ImdbVotes { get; set; }
        public string? ImdbId { get; set; }
        public string? BoxOffice { get; set; }
        public string? Production { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            var lines = Ratings.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = 0; index < lines.Length; index++) {
                var parts = lines[index].Split('|', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) {
                    yield return new ValidationResult(
                        $"Rating on line {index + 1} must contain a source and value separated by |.",
                        [nameof(Ratings)]);
                }
            }
        }

        public void ApplyTo(Movie movie) {
            movie.Title = Title;
            movie.Year = Year;
            movie.Rated = Rated;
            movie.Released = Released!.Value;
            movie.Runtime = Runtime;
            movie.Genres = ParseLines(Genres);
            movie.Director = Director;
            movie.Writer = Writer;
            movie.Actors = ParseLines(Actors);
            movie.Plot = Plot;
            movie.Language = Language;
            movie.Country = Country;
            movie.Awards = Awards;
            movie.Poster = Poster;
            movie.Ratings = Ratings.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split('|', 2, StringSplitOptions.TrimEntries))
                .Select(parts => new MovieRating { Source = parts[0], Value = parts[1] })
                .ToList();
            movie.Metascore = Metascore;
            movie.ImdbRating = ImdbRating;
            movie.ImdbVotes = ImdbVotes;
            movie.ImdbId = ImdbId;
            movie.BoxOffice = BoxOffice;
            movie.Production = Production;
        }

        public static MovieFormViewModel FromMovie(Movie movie) => new() {
            Id = movie.Id,
            Title = movie.Title,
            Year = movie.Year,
            Rated = movie.Rated,
            Released = movie.Released,
            Runtime = movie.Runtime,
            Genres = string.Join(Environment.NewLine, movie.Genres),
            Director = movie.Director,
            Writer = movie.Writer,
            Actors = string.Join(Environment.NewLine, movie.Actors),
            Plot = movie.Plot,
            Language = movie.Language,
            Country = movie.Country,
            Awards = movie.Awards,
            Poster = movie.Poster,
            Ratings = string.Join(Environment.NewLine, movie.Ratings.Select(rating => $"{rating.Source} | {rating.Value}")),
            Metascore = movie.Metascore,
            ImdbRating = movie.ImdbRating,
            ImdbVotes = movie.ImdbVotes,
            ImdbId = movie.ImdbId,
            BoxOffice = movie.BoxOffice,
            Production = movie.Production
        };

        private static List<string> ParseLines(string value) => value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
