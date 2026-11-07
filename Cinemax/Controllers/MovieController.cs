using Cinemax.Data;
using Cinemax.Models;
using Cinemax.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Controllers
{
    
    public class MovieController : Controller
    {
        private const int PageSize = 24;
        private readonly ApplicationDbContext _context;
        //constructor to instantiate the db context
        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        //displays all data for the index view
        public IActionResult Index(string? q = null, int? genreId = null, int page = 1)
        {
            var query = MovieSearch.Matching(_context.Movies.Include(m => m.Genre).Include(m => m.Reviews), q);
            if (genreId.HasValue)
            {
                query = query.Where(m => m.GenreId == genreId);
            }
            var total = query.Count();
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, pages);
            ViewBag.Query = q;
            ViewBag.GenreId = genreId;
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.Pages = pages;
            ViewBag.Genres = _context.Genres.OrderBy(g => g.Name).ToList();
            return View("Index", query.OrderBy(m => m.MovieId).Skip((page - 1) * PageSize).Take(PageSize).ToList());
        }

        public IActionResult Details(int id)
        {
            var movie = _context.Movies.Include(m => m.Genre).Include(m => m.Reviews).FirstOrDefault(m => m.MovieId == id);
            if (movie == null)
            {
                return NotFound();
            }
            ViewBag.Related = _context.Movies.Include(m => m.Reviews)
                .Where(m => m.GenreId == movie.GenreId && m.MovieId != id)
                .OrderByDescending(m => m.TmdbRating).Take(12).ToList();
            return View("Details", movie);
        }

        //create method to return the create view
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(_context.Genres.OrderBy(g => g.Name).ToList(), "GenreId", "Name");
            return View("Create");
        }


        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //post method to create a movie 
        public IActionResult Create([Bind("MovieId,MovieTitle,Director,ReleaseYear,Duration,GenreId,Overview,TopCast")] Movie movie)
        {
            
            if (!_context.Genres.Any(g => g.GenreId == movie.GenreId))
                ModelState.AddModelError(nameof(movie.GenreId), "Choose an existing genre.");
            movie.MovieId = 0;
            RejectDuplicate(movie);
            //checks if the input is valid
            if (!ModelState.IsValid)
            {
                ViewBag.GenreId = new SelectList(_context.Genres.OrderBy(g => g.Name).ToList(), "GenreId", "Name"); 

                return View("Create",movie);
            }

            //adds the movie to the db
            _context.Movies.Add(movie);
            //saves the changes to the db
            _context.SaveChanges();
            TempData["Success"] = $"{movie.MovieTitle} was added to Cinemax.";
            //redirects to the user to the index 
            return RedirectToAction("Index");
        }
        //get method to edit a movie
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(int id)
        {
            //create a variable to hold the movie and find it by id
            var movie = _context.Movies.Find(id);

            //checks if the movie is null
            if (movie == null)
            {
                return NotFound();
            }
            ViewBag.GenreId = new SelectList(_context.Genres.OrderBy(g => g.Name).ToList(), "GenreId", "Name");
            //return the movie to the view
            return View("Edit",movie);
        }
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //post method to edit a movie
        public IActionResult Edit(int id, [Bind("MovieId,MovieTitle,Director,ReleaseYear,Duration,GenreId,Overview,TopCast")] Movie movie)
        {
            if (id != movie.MovieId) return BadRequest();
            var existing = _context.Movies.Find(id);
            if (existing == null) return NotFound();
            if (!_context.Genres.Any(g => g.GenreId == movie.GenreId))
                ModelState.AddModelError(nameof(movie.GenreId), "Choose an existing genre.");
            RejectDuplicate(movie);
            // validate the input of the user
            if (!ModelState.IsValid)
            {
                ViewBag.GenreId = new SelectList(_context.Genres.OrderBy(g => g.Name).ToList(), "GenreId", "Name");
                return View("Edit",movie);
            }

            //update movie in the db
            existing.MovieTitle = movie.MovieTitle;
            existing.Director = movie.Director;
            existing.ReleaseYear = movie.ReleaseYear;
            existing.Duration = movie.Duration;
            existing.GenreId = movie.GenreId;
            existing.Overview = movie.Overview;
            existing.TopCast = movie.TopCast;
            _context.SaveChanges();
            TempData["Success"] = $"{movie.MovieTitle} was updated.";

            // redirect to list on the Index
            return RedirectToAction("Index");

        }
        //get method to delete a movie
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null)
            {
                return NotFound();
            }
            //remove movie from db
            _context.Movies.Remove(movie);
            //save changes to db
            _context.SaveChanges();
            TempData["Success"] = $"{movie.MovieTitle} was removed.";
            //redirect the user to the index
            return RedirectToAction("Index");
        }

        private void RejectDuplicate(Movie movie)
        {
            var title = movie.MovieTitle?.Trim().ToLower();
            if (title != null && _context.Movies.Any(m => m.MovieId != movie.MovieId
                && m.ReleaseYear == movie.ReleaseYear && m.MovieTitle!.ToLower() == title))
            {
                ModelState.AddModelError(nameof(movie.MovieTitle), $"{movie.MovieTitle} ({movie.ReleaseYear}) is already in Cinemax.");
            }
        }

    }

}