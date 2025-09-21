using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Review
    {
        public string? ReviewerName { get; set; }
        public int ReviewId { get; set; }
        public string? ReviewMessage { get; set; }
        public int Rating { get; set; }

        //foreign key, one movie can have many reviews
        public int MovieId { get; set; }
    }
}
