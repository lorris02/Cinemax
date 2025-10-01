

using Cinemax.Data;

namespace Cinemax.Controllers
{
    public class GenreController
    {
        //constructor to instantiate the db context
        private readonly ApplicationDbContext _context;
        public GenreController(ApplicationDbContext context)
        {
            _context = context;
        }
    }
}
