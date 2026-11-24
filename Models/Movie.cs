namespace NornavindCinema.Models {
    public class Movie {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? Rated { get; set; }
        public DateOnly? Released { get; set; }
        public string? Runtime { get; set; }
        public List<string> Genres { get; set; } = [];
        public string? Director { get; set; }
        public string? Writer { get; set; }
        public List<string> Actors { get; set; } = [];
        public string? Plot { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public string? Awards { get; set; }
        public string? Poster { get; set; }
        public List<MovieRating> Ratings { get; set; } = [];
        public string? Metascore { get; set; }
        public string? ImdbRating { get; set; }
        public string? ImdbVotes { get; set; }
        public string? ImdbId { get; set; }
        public string? BoxOffice { get; set; }
        public string? Production { get; set; }
    }

    public class MovieRating {
        public string? Source { get; set; }
        public string? Value { get; set; }
    }
}
