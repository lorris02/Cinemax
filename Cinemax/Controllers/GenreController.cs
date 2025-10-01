

using Cinemax.Data;
using Microsoft.AspNetCore.Mvc;

namespace Cinemax.Controllers
{
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
            return View();
        }
    }
}
