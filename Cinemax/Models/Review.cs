using System.ComponentModel.DataAnnotations;

namespace Cinemax.Models
{
    public class Review
    {
        [Required, StringLength(100)]
        public string? ReviewerName { get; set; }
        public int ReviewId { get; set; }
        [Required, StringLength(2000)]
        public string? ReviewMessage { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [Range(1, int.MaxValue)]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }
    }
}
