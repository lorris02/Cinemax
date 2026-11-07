using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Movie : IValidatableObject
    {
        public int MovieId { get; set; }
        [Required(ErrorMessage = "Enter the movie title."), StringLength(200)]
        public string? MovieTitle { get; set; }
        [Required(ErrorMessage = "Enter the director's name."), StringLength(150)]
        public string? Director { get; set; }
        [Required(ErrorMessage = "Enter the release year.")]
        public int ReleaseYear { get; set; }
        [Required(ErrorMessage = "Enter the runtime in minutes.")]
        [Range(1, 1440, ErrorMessage = "Enter a runtime from 1 to 1440 minutes.")]
        public int Duration { get; set; }
        [NotMapped]
        public string DurationFormatted => Duration > 0 ? $"{Duration / 60}h {Duration % 60}m" : "Runtime unknown";
        [Range(1, int.MaxValue)]
        public int GenreId { get; set; }
        public Genre? Genre { get; set; }
        public int? TmdbId { get; set; }
        [StringLength(2000)]
        public string? Overview { get; set; }
        [StringLength(300)]
        public string? TopCast { get; set; }
        [StringLength(100)]
        public string? PosterPath { get; set; }
        [StringLength(100)]
        public string? BackdropPath { get; set; }
        public double? TmdbRating { get; set; }
        public bool TmdbDetailsLoaded { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public string? PosterUrl => PosterPath == null ? null : $"https://image.tmdb.org/t/p/w500{PosterPath}";
        [NotMapped]
        public string? BackdropUrl => BackdropPath == null ? null : $"https://image.tmdb.org/t/p/w1280{BackdropPath}";
        [NotMapped]
        public double? AverageRating => Reviews.Count == 0 ? null : Reviews.Average(r => r.Rating);
        [NotMapped]
        public double? CardScore => TmdbRating ?? AverageRating * 2;
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (ReleaseYear < 1888 || ReleaseYear > DateTime.UtcNow.Year + 2)
                yield return new ValidationResult("Choose a release year from 1888 through the next two years.", new[] { nameof(ReleaseYear) });
        }
    }
}
