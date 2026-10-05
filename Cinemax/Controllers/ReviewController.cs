using Cinemax.Data;
using Cinemax.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Controllers
{
    
    public class ReviewController : Controller
    {
        //constructor to instantiate the db context
        private readonly ApplicationDbContext _context;
        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // fetch list of reviews from db including movie info, and order it by reviewer name
            var reviews = _context.Reviews
                .OrderBy(r => r.ReviewerName)
                .Include(r => r.Movie)
                .ToList();

            // display the list of reviews
            return View(reviews);
        }

        //get method to return an empty form for the create view
        [Authorize]
        public IActionResult Create()
        {
            // fetch list of movietitles for the movieid dropdown in the form
            ViewBag.MovieId = new SelectList(_context.Movies.ToList(), "MovieId","MovieTitle");

            // display empty form so users can add a review
            return View();
        }

        // post method to add a review
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Create([Bind("ReviewerName,ReviewMessage,ReviewId,Rating,MovieId")] Review review)
        {
            review.ReviewId = 0;
            if (!_context.Movies.Any(m => m.MovieId == review.MovieId))
                ModelState.AddModelError(nameof(review.MovieId), "Choose an existing movie.");
            // validate inputs
            if (!ModelState.IsValid)
            {
                // fetch list of movies for movieid dropdown in the form
                ViewBag.MovieId = new SelectList(_context.Movies.ToList(), "MovieId","MovieTitle");
                return View(review);
            }

            // save to db then redirect to index
            _context.Reviews.Add(review);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //get method to return a populated form for the edit view
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(int id)
        {
            var review = _context.Reviews.Find(id);

            if (review == null)
            {
                return NotFound();
            }

            // fetch list of movies for movieid dropdown in the form
            ViewBag.MovieId = new SelectList(_context.Movies.ToList(), "MovieId","MovieTitle");

            // display populated form to edit a review
            return View(review);
        }


        //post method to save the edited review
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("ReviewerName,ReviewMessage,ReviewId,Rating,MovieId")] Review review)
        {
            if (id != review.ReviewId) return BadRequest();
            var existing = _context.Reviews.Find(id);
            if (existing == null) return NotFound();
            if (!_context.Movies.Any(m => m.MovieId == review.MovieId))
                ModelState.AddModelError(nameof(review.MovieId), "Choose an existing movie.");
            // validate the input
            if (!ModelState.IsValid)
            {
                /// fetch list of movies for movieid dropdown in the form,and it selects the movie that was originally selected
                ViewBag.MovieId = new SelectList(_context.Movies.ToList(), "MovieId", "MovieTitle",review.MovieId);

                // show the edit form with info the user provided
                return View(review);
            }

            // save and redirect to index
            existing.ReviewerName = review.ReviewerName;
            existing.ReviewMessage = review.ReviewMessage;
            existing.Rating = review.Rating;
            existing.MovieId = review.MovieId;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //get method to confirm deletion of a review
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var review = _context.Reviews.Find(id);

            if (review == null)
            {
                return NotFound();
            }

            // delete and then redirect to index
            _context.Reviews.Remove(review);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}