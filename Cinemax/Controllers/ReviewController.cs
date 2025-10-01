

using Cinemax.Data;

namespace Cinemax.Controllers
{
    public class ReviewController 
    {
        //constructor to instantiate the db context
        private readonly ApplicationDbContext _context;
        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }


    }
}