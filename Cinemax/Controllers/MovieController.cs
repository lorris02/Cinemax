using Cinemax.Data;

namespace Cinemax.Controllers
{
    public class MovieController
    {
        private readonly ApplicationDbContext _context;
        //constructor to instantiate the db context
        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }
    }
}