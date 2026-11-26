namespace NornavindCinema.ViewModels {
    public class AdminUserViewModel {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public IList<string> Roles { get; set; } = [];
    }
}
