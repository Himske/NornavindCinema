using System.ComponentModel.DataAnnotations;

namespace NornavindCinema.ViewModels {
    public class ApiConfigurationViewModel : IValidatableObject {
        [Display(Name = "OMDB API Key")]
        [StringLength(4096)]
        public string? OmdbApiKey { get; set; }
        public bool ClearOmdbApiKey { get; set; }
        public bool HasOmdbApiKey { get; set; }

        [Display(Name = "TMDB API Key")]
        [StringLength(4096)]
        public string? TmdbApiKey { get; set; }
        public bool ClearTmdbApiKey { get; set; }
        public bool HasTmdbApiKey { get; set; }

        [Display(Name = "TMDB API Read Access Token")]
        [StringLength(4096)]
        public string? TmdbReadAccessToken { get; set; }
        public bool ClearTmdbReadAccessToken { get; set; }
        public bool HasTmdbReadAccessToken { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (ClearOmdbApiKey && !string.IsNullOrWhiteSpace(OmdbApiKey)) {
                yield return new ValidationResult("Enter a new OMDB API key or remove it, not both.", [nameof(OmdbApiKey)]);
            }
            if (ClearTmdbApiKey && !string.IsNullOrWhiteSpace(TmdbApiKey)) {
                yield return new ValidationResult("Enter a new TMDB API key or remove it, not both.", [nameof(TmdbApiKey)]);
            }
            if (ClearTmdbReadAccessToken && !string.IsNullOrWhiteSpace(TmdbReadAccessToken)) {
                yield return new ValidationResult("Enter a new TMDB read access token or remove it, not both.", [nameof(TmdbReadAccessToken)]);
            }
        }
    }
}
