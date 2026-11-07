using System.ComponentModel.DataAnnotations;

namespace Cinemax.Models
{
    public class Review
    {
        [Required(ErrorMessage = "Enter the name to show with your review."), StringLength(100)]
        public string? ReviewerName { get; set; }
        public int ReviewId { get; set; }
        [Required(ErrorMessage = "Write your review."), StringLength(2000)]
        public string? ReviewMessage { get; set; }
        [Range(1, 5, ErrorMessage = "Choose a star rating.")]
        public int Rating { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Choose a movie.")]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }
        [StringLength(50)]
        public string? TmdbReviewId { get; set; }
        [StringLength(200)]
        public string? SourceUrl { get; set; }
    }
}
