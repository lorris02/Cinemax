namespace Cinemax.Models
{
    public class HomeViewModel
    {
        public Movie? Featured { get; set; }
        public IReadOnlyList<Movie> Movies { get; set; } = Array.Empty<Movie>();
        public IReadOnlyList<Review> LatestReviews { get; set; } = Array.Empty<Review>();
    }
}
