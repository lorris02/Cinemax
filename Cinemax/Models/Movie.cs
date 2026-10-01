using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Movie : IValidatableObject
    {
        public int MovieId { get; set; }
        [Required, StringLength(200)]
        public string? MovieTitle { get; set; }
        [Required, StringLength(150)]
        public string? Director { get; set; }
        public int ReleaseYear { get; set; }
        [Range(1, 1440)]
        public int Duration { get; set; }
        [NotMapped]
        public string DurationFormatted => $"{Duration / 60}h {Duration % 60}m";
        [Range(1, int.MaxValue)]
        public int GenreId { get; set; }
        public Genre? Genre { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (ReleaseYear < 1888 || ReleaseYear > DateTime.UtcNow.Year + 2)
                yield return new ValidationResult("Choose a release year from 1888 through the next two years.", new[] { nameof(ReleaseYear) });
        }
    }
}
