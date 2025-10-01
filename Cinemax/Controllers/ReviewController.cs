

using Cinemax.Data;
using Microsoft.AspNetCore.Mvc;

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

        public  IActionResult Index()
        {
         return View();
        }
    }
}