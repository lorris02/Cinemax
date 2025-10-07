using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Movie
    {
        public int MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public string? Director { get; set; }

        [Range(1980,2024, ErrorMessage = "Please we don't accept movies with a Releaseyear before 1980 and after 2024")]
        public int ReleaseYear { get; set; }
        
        public int Duration { get; set; }

        //Added a notmapped property to make it read only so i format the duration in hours and minutes
        [NotMapped] 
        public string DurationFormatted
        {
            get
            {
                return $"{Duration / 60}h {Duration % 60}m";
            }
        }

        //Genre foreign key, one genre can have many movies
        public int GenreId { get; set; }
        //navigation property
        public Genre? Genre { get; set; }

        List<Review> Review { get; set; } = new();

    }
}
