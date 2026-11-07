using Cinemax.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Cinemax.Controllers
{  //i added the edit and delete methods before i creates genres just incase i need to edit them later,but afterwards i'm going to make the genres fixed and they cant be edited or created
    public class GenreController : Controller
    {
        //constructor to instantiate the db context
        private readonly ApplicationDbContext _context;
        public GenreController(ApplicationDbContext context)
        {
            _context = context;
        }

        //index method to return the view
        public IActionResult Index()
        {
            var genres = _context.Genres.OrderBy(g => g.Name).ToList();
            var movies = _context.Movies.Select(m => new Models.Movie { GenreId = m.GenreId, BackdropPath = m.BackdropPath, TmdbRating = m.TmdbRating }).ToList();
            ViewBag.Covers = movies.GroupBy(m => m.GenreId).ToDictionary(group => group.Key, group => (
                group.OrderByDescending(m => m.TmdbRating).FirstOrDefault(m => m.BackdropPath != null)?.BackdropUrl,
                group.Count()));
            return View(genres);
        }
        //Get method to return the create view
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            return View();
        }
        //post method to create a genre
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Name,Description")] Models.Genre genre)
        {
            RejectDuplicate(genre);
            if (!ModelState.IsValid)
            {
                return View(genre);
            }
            _context.Genres.Add(genre);
            _context.SaveChanges();
            TempData["Success"] = $"{genre.Name} was added.";
            return RedirectToAction("Index");
        }
        //get method to edit a genre
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(int id)
        {
            var genre = _context.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }
        //post method to edit a genre
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("GenreId,Name,Description")] Models.Genre genre)
        {
            if (id != genre.GenreId) return BadRequest();
            var existing = _context.Genres.Find(id);
            if (existing == null) return NotFound();
            RejectDuplicate(genre);
            if (!ModelState.IsValid)
            {
                return View(genre);
            }
            existing.Name = genre.Name;
            existing.Description = genre.Description;
            _context.SaveChanges();
            TempData["Success"] = $"{genre.Name} was updated.";
            return RedirectToAction("Index");
        }
        // Genres in use must remain available to their movies.
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var genre = _context.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }
            if (_context.Movies.Any(m => m.GenreId == id))
            {
                TempData["Notice"] = $"{genre.Name} still has movies, so it can't be removed.";
                return RedirectToAction("Index");
            }
            _context.Genres.Remove(genre);
            _context.SaveChanges();
            TempData["Success"] = $"{genre.Name} was removed.";
            return RedirectToAction("Index");
        }

        private void RejectDuplicate(Models.Genre genre)
        {
            var name = genre.Name?.Trim().ToLower();
            if (name != null && _context.Genres.Any(g => g.GenreId != genre.GenreId && g.Name!.ToLower() == name))
            {
                ModelState.AddModelError(nameof(genre.Name), $"{genre.Name} is already a genre.");
            }
        }
    }
}
