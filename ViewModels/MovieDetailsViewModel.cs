using System.ComponentModel.DataAnnotations;
using NornavindCinema.Models;

namespace NornavindCinema.ViewModels {
    public class MovieReviewInput : IValidatableObject {
        [Range(1, 10)]
        public int? Rating { get; set; }

        [StringLength(2000)]
        [Display(Name = "Review")]
        public string? Review { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Rating is null && string.IsNullOrWhiteSpace(Review)) {
                yield return new ValidationResult("Enter a review or choose a rating.", [nameof(Review)]);
            }
        }
    }

    public class MovieDetailsViewModel {
        public Movie Movie { get; set; } = null!;
        public bool IsFavorite { get; set; }
        public bool HasOwnReview { get; set; }
        public bool OwnReviewIsHidden { get; set; }
        public bool OpenReviewModal { get; set; }
        public MovieReviewInput Feedback { get; set; } = new();
        public List<MemberMovieReview> Reviews { get; set; } = [];
    }
}
