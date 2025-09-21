namespace Cinemax.Models
{
    public class Movie
    {
       public int MovieId { get; set; }
       public string? MovieTitle { get; set; }
       public string? Director { get; set; }    
       public int ReleaseYear { get; set; }
       public int Duration { get; set; }
        //Genre foreign key, one genre can have many movies
       public int GenreId { get; set; }

    }
}
