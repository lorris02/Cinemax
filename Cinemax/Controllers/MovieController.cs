using Cinemax.Data;
using Microsoft.AspNetCore.Mvc;

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

        //index method to return the view
        public IActionResult Index()
        {
            return View();
        }
    }
}