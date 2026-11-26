using NornavindCinema.Data;

namespace NornavindCinema.Models {
    public class MovieFavorite {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
    }
}
