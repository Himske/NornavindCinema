using NornavindCinema.Data;

namespace NornavindCinema.ViewModels {
    public class OmdbSearchViewModel {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int TotalResults { get; set; }
        public List<OmdbSearchItem> Results { get; set; } = [];
        public string? Error { get; set; }
    }
}
