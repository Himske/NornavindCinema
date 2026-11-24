using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NornavindCinema.Data;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    public class AdminController(MovieSeeder movieSeeder) : Controller {
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedMovies(CancellationToken cancellationToken) {
            var result = await movieSeeder.SeedAsync(cancellationToken);
            TempData["SeedResult"] = $"Added {result.Added} movies; skipped {result.Skipped} already present or duplicate entries.";
            return RedirectToAction(nameof(Index));
        }
    }
}
