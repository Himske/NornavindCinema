using System.ComponentModel.DataAnnotations;

namespace NornavindCinema.Models {
    public class ApiCredential {
        [Key]
        [MaxLength(64)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ProtectedValue { get; set; } = string.Empty;
    }
}
