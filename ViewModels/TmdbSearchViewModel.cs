using NornavindCinema.Data;

namespace NornavindCinema.ViewModels {
    public class TmdbSearchViewModel {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
        public List<TmdbMovie> Results { get; set; } = [];
        public string? Error { get; set; }
    }
}
