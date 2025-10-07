using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Review
    {
        public string? ReviewerName { get; set; }
        public int ReviewId { get; set; }
        public string? ReviewMessage { get; set; }
        [Range(0, 5)]
        public int Rating { get; set; }

        //foreign key, one movie can have many reviews
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }
       


    }
}
