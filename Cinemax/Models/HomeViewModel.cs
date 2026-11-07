namespace Cinemax.Models
{
    public record GenreRow(Genre Genre, IReadOnlyList<Movie> Movies);

    public class HomeViewModel
    {
        public Movie? Featured { get; set; }
        public IReadOnlyList<Movie> Movies { get; set; } = Array.Empty<Movie>();
        public IReadOnlyList<Review> LatestReviews { get; set; } = Array.Empty<Review>();
        public IReadOnlyList<GenreRow> GenreRows { get; set; } = Array.Empty<GenreRow>();
    }
}
