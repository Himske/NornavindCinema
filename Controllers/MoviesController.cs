using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.Models;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    public class MoviesController(ApplicationDbContext context) : Controller {
        public async Task<IActionResult> Index() {
            var movies = await context.Movies
                .AsNoTracking()
                .OrderBy(movie => movie.Title)
                .ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Details(int? id) {
            if (id is null) {
                return NotFound();
            }

            var movie = await context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.Id == id);
            return movie is null ? NotFound() : View(movie);
        }

        [Authorize(Policy = "Administrator")]
        public IActionResult Create() => View(new MovieFormViewModel());

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel form) {
            if (!ModelState.IsValid) {
                return View(form);
            }

            var movie = new Movie();
            form.ApplyTo(movie);
            context.Movies.Add(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Edit(int? id) {
            if (id is null) {
                return NotFound();
            }

            var movie = await context.Movies.AsNoTracking().FirstOrDefaultAsync(movie => movie.Id == id);
            return movie is null ? NotFound() : View(MovieFormViewModel.FromMovie(movie));
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieFormViewModel form) {
            if (!ModelState.IsValid) {
                return View(form);
            }

            var movie = await context.Movies.FirstOrDefaultAsync(movie => movie.Id == id);
            if (movie is null) {
                return NotFound();
            }

            form.ApplyTo(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Administrator")]
        public async Task<IActionResult> Delete(int? id) {
            if (id is null) {
                return NotFound();
            }

            var movie = await context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.Id == id);
            return movie is null ? NotFound() : View(movie);
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var movie = await context.Movies.FindAsync(id);
            if (movie is null) {
                return NotFound();
            }

            context.Movies.Remove(movie);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
