using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.Models;
using NornavindCinema.ViewModels;
using System.Diagnostics;
using System.Globalization;

namespace NornavindCinema.Controllers {
    public class HomeController : Controller {
        private const int HeroCount = 5;
        private const int RowSize = 30;

        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) {
            _context = context;
        }

        public async Task<IActionResult> Index() {
            var movies = await _context.Movies
                .AsNoTracking()
                .Select(m => new {
                    m.Id,
                    m.Title,
                    m.Poster,
                    m.Backdrop,
                    m.Runtime,
                    m.Plot,
                    m.ImdbRating,
                    m.ImdbVotes,
                    m.Released,
                    m.Year,
                    m.Genres
                })
                .ToListAsync();

            var items = movies.Select(m => new {
                Card = new HomeMovieCard {
                    Id = m.Id,
                    Title = m.Title,
                    Poster = Usable(m.Poster),
                    Backdrop = Usable(m.Backdrop) ?? Usable(m.Poster),
                    Runtime = Usable(m.Runtime),
                    Plot = Usable(m.Plot),
                    Rating = Usable(m.ImdbRating),
                    Year = Usable(m.Year),
                    Genres = m.Genres ?? []
                },
                HasBackdrop = Usable(m.Backdrop) is not null,
                Rating = ParseDouble(m.ImdbRating),
                Votes = ParseVotes(m.ImdbVotes),
                m.Released
            }).ToList();
            var posterItems = items.Where(x => x.Card.Poster is not null).ToList();

            var model = new HomeViewModel {
                Hero = items.Where(x => x.Card.Plot is not null && x.Card.Backdrop is not null)
                    .OrderByDescending(x => x.HasBackdrop).ThenByDescending(x => x.Rating).ThenByDescending(x => x.Votes)
                    .Take(HeroCount).Select(x => x.Card).ToList(),
                Popular = posterItems.OrderByDescending(x => x.Votes).Take(RowSize).Select(x => x.Card).ToList(),
                Premieres = posterItems.Where(x => x.Released.HasValue)
                    .OrderByDescending(x => x.Released).Take(RowSize).Select(x => x.Card).ToList(),
                RecentlyAdded = posterItems.OrderByDescending(x => x.Card.Id).Take(RowSize).Select(x => x.Card).ToList(),
                Genres = posterItems.SelectMany(x => x.Card.Genres)
                    .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count()).Take(12)
                    .Select(g => g.Key).OrderBy(g => g).ToList()
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string? Usable(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : value.Trim();

        private static double ParseDouble(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0;

        private static long ParseVotes(string? value) =>
            long.TryParse(value?.Replace(",", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
