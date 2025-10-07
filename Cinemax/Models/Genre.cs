using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinemax.Models
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string? Name { get; set; }

        public string? Description { get; set; }

    } 
}
