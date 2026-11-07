using System.Diagnostics;
using Cinemax.Data;
using Cinemax.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Controllers
{
    public class HomeController : Controller
    {
        private const int RowsShown = 3;
        private const int MinimumRowSize = 4;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var movies = _context.Movies.Include(m => m.Genre).Include(m => m.Reviews)
                .OrderBy(m => m.MovieId).Take(15).ToList();
            var topGenres = _context.Movies.GroupBy(m => m.GenreId)
                .Select(g => new { GenreId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count).Take(RowsShown).ToList();
            var rows = topGenres.Select(top => new GenreRow(
                _context.Genres.First(g => g.GenreId == top.GenreId),
                _context.Movies.Include(m => m.Reviews).Where(m => m.GenreId == top.GenreId)
                    .OrderByDescending(m => m.TmdbRating).Take(14).ToList()))
                .Where(row => row.Movies.Count >= MinimumRowSize).ToList();
            var reviews = _context.Reviews.Include(r => r.Movie)
                .OrderBy(r => r.TmdbReviewId != null).ThenByDescending(r => r.ReviewId).Take(60)
                .AsEnumerable().DistinctBy(r => r.MovieId).Take(3).ToList();
            return View(new HomeViewModel {
                Featured = movies.FirstOrDefault(m => m.BackdropPath != null),
                Movies = movies,
                LatestReviews = reviews,
                GenreRows = rows
            });
        }

        public IActionResult Status(int id)
        {
            ViewBag.Code = id;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
