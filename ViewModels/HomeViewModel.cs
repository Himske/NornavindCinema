namespace NornavindCinema.ViewModels {
    public class HomeMovieCard {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? Runtime { get; set; }
        public string? Plot { get; set; }
        public string? Rating { get; set; }
        public string? Year { get; set; }
        public List<string> Genres { get; set; } = [];
    }

    public class HomeViewModel {
        public List<HomeMovieCard> Hero { get; set; } = [];
        public List<HomeMovieCard> Popular { get; set; } = [];
        public List<HomeMovieCard> Premieres { get; set; } = [];
        public List<HomeMovieCard> RecentlyAdded { get; set; } = [];
        public List<string> Genres { get; set; } = [];
    }
}
