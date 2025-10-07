using Cinemax.Data;
using Cinemax.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        //constructor to instantiate the db context
        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        //displays all data for the index view
        public IActionResult Index()
        {
            var movies = _context.Movies.Include(m => m.Genre).ToList();
            return View(movies);
        }
        //create method to return the create view
        public IActionResult Create()
        {
            ViewBag.GenreId = new SelectList(_context.Genres.ToList(), "GenreId", "Name");
            return View();
        }


        [HttpPost]
        //post method to create a movie 
        public IActionResult Create([Bind("MovieId,MovieTitle,Director,ReleaseYear,Duration,GenreId")] Movie movie)
        {
            
            //checks if the input is valid
            if (!ModelState.IsValid)
            {
                ViewBag.GenreId = new SelectList(_context.Genres.ToList(), "GenreId", "Name"); 

                return View();
            }

            //adds the movie to the db
            _context.Movies.Add(movie);
            //saves the changes to the db
            _context.SaveChanges();
            //redirects to the user to the index 
            return RedirectToAction("Index");
        }
        //get method to edit a movie
        public IActionResult Edit(int id)
        {
            //create a variable to hold the movie and find it by id
            var movie = _context.Movies.Find(id);

            //checks if the movie is null
            if (movie == null)
            {
                return NotFound();
            }
            ViewBag.GenreId = new SelectList(_context.Genres.ToList(), "GenreId", "Name");
            //return the movie to the view
            return View(movie);
        }
        [HttpPost]
        //post method to edit a movie
        public IActionResult Edit(int id, [Bind("MovieId,MovieTitle,Director,ReleaseYear,Duration,GenreId")] Movie movie)
        {
            // validate the input of the user
            if (!ModelState.IsValid)
            {
                ViewBag.GenreId = new SelectList(_context.Genres.ToList(), "GenreId","Name");
                return View();
            }

            //update movie in the db
            _context.Movies.Update(movie);
            _context.SaveChanges();

            // redirect to list on the Index
            return RedirectToAction("Index");

        }
        //get method to delete a movie
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
            //redirect the user to the index
            return RedirectToAction("Index");
        }

    }

}