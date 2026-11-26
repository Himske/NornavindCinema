using System.ComponentModel.DataAnnotations;
using NornavindCinema.Data;

namespace NornavindCinema.Models {
    public class MemberMovieReview {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
        public int? Rating { get; set; }
        [MaxLength(2000)]
        public string? Review { get; set; }
    }
}
