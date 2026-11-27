namespace NornavindCinema.ViewModels {
    public class AdminDashboardViewModel {
        public int MovieCount { get; set; }
        public int UserCount { get; set; }
        public int ReviewCount { get; set; }
        public DashboardTopMovie? TopRated { get; set; }
        public List<DashboardGenre> TopGenres { get; set; } = [];
        public List<DashboardMovie> RecentMovies { get; set; } = [];
    }

    public record DashboardTopMovie(int MovieId, string? Title, double AverageRating, int RatingCount);
    public record DashboardGenre(string Name, int Count);
    public record DashboardMovie(int Id, string? Title);
}
