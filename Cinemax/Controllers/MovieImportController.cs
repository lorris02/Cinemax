using Cinemax.Data;
using Cinemax.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinemax.Controllers
{
    [Authorize(Roles = DatabaseSetup.AdminRole)]
    public class MovieImportController : Controller
    {
        private readonly ITmdbClient _tmdb;
        private readonly MovieImporter _importer;

        public MovieImportController(ITmdbClient tmdb, MovieImporter importer)
        {
            _tmdb = tmdb;
            _importer = importer;
        }

        public async Task<IActionResult> Index(string? q = null)
        {
            ViewBag.Query = q;
            ViewBag.Enabled = _tmdb.Enabled;
            try
            {
                var results = string.IsNullOrWhiteSpace(q) ? await _tmdb.PopularAsync() : await _tmdb.SearchAsync(q.Trim());
                return View(results);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                ViewBag.Error = "TMDB could not be reached. Please try again.";
                return View(Array.Empty<TmdbMovieSummary>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int tmdbId)
        {
            try
            {
                var movie = await _importer.ImportAsync(tmdbId);
                if (movie != null)
                {
                    return RedirectToAction("Details", "Movie", new { id = movie.MovieId });
                }
                TempData["ImportError"] = "That movie is missing details Cinemax needs, such as a runtime.";
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                TempData["ImportError"] = "TMDB could not be reached. Please try again.";
            }
            return RedirectToAction("Index");
        }
    }
}
