using Cinemax.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinemax.Controllers
{
    public class SearchController : Controller
    {
        private const int MinLength = 2;
        private const int Shown = 6;
        private readonly MovieSearch _search;
        private readonly MovieImporter _importer;

        public SearchController(MovieSearch search, MovieImporter importer)
        {
            _search = search;
            _importer = importer;
        }

        [EnableRateLimiting("search")]
        public async Task<IActionResult> Suggest(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinLength)
            {
                return Json(new { local = Array.Empty<object>(), tmdb = Array.Empty<object>() });
            }
            var local = _search.Local(q, Shown).Select(m => new {
                title = m.MovieTitle,
                year = m.ReleaseYear.ToString(),
                poster = m.PosterUrl,
                url = Url.Action("Details", "Movie", new { id = m.MovieId })
            });
            return Json(new { local, tmdb = await _search.FromTmdbAsync(q, Shown) });
        }

        // Visitors can open any TMDB result, so importing is limited per address.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("import")]
        public async Task<IActionResult> Open(int tmdbId)
        {
            try
            {
                var movie = await _importer.ImportAsync(tmdbId);
                if (movie != null)
                {
                    return RedirectToAction("Details", "Movie", new { id = movie.MovieId });
                }
                TempData["Notice"] = "That movie is missing details Cinemax needs, so it can't be shown.";
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                TempData["Notice"] = "TMDB could not be reached. Please try again.";
            }
            return RedirectToAction("Index", "Movie");
        }
    }
}
