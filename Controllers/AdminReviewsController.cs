using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminReviewsController(ApplicationDbContext context) : Controller {
        public async Task<IActionResult> Index() {
            var reviews = await context.MemberMovieReviews.AsNoTracking()
                .Include(review => review.Movie)
                .Include(review => review.User)
                .OrderBy(review => review.Movie.Title).ThenBy(review => review.User.Email)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetVisibility(string userId, int movieId, bool? hidden) {
            if (string.IsNullOrEmpty(userId) || movieId <= 0 || hidden is null) return BadRequest();
            var review = await context.MemberMovieReviews.FindAsync(userId, movieId);
            if (review is null) return NotFound();
            review.IsHidden = hidden.Value;
            await context.SaveChangesAsync();
            TempData["ModerationSuccess"] = hidden.Value ? "Review and rating hidden." : "Review and rating restored.";
            return RedirectToAction(nameof(Index));
        }
    }
}
