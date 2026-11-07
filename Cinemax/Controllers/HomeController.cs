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
                .OrderBy(m => m.MovieId).Take(13).ToList();
            var reviews = _context.Reviews.Include(r => r.Movie)
                .OrderByDescending(r => r.ReviewId).Take(3).ToList();
            return View(new HomeViewModel {
                Featured = movies.FirstOrDefault(m => m.BackdropPath != null),
                Movies = movies,
                LatestReviews = reviews
            });
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
