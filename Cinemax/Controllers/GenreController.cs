using Cinemax.Data;
using Microsoft.AspNetCore.Mvc;

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
            var genres = _context.Genres.ToList();
            return View(genres);
        }
        //Get method to return the create view
        public IActionResult Create()
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            return View();
        }
        //post method to create a genre
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Name,Description")] Models.Genre genre)
        {
            if (!ModelState.IsValid)
            {
                return View(genre);
            }
            _context.Genres.Add(genre);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        //get method to edit a genre
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("GenreId,Name,Description")] Models.Genre genre)
        {
            if (!ModelState.IsValid)
            {
                return View(genre);
            }
            _context.Genres.Update(genre);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        //delete method to delete a genre
        public IActionResult Delete(int id)
        {
            var genre = _context.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }
    }
}
