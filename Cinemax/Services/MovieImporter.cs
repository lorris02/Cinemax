using System.Text.RegularExpressions;
using Cinemax.Data;
using Cinemax.Models;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Services
{
    public class MovieImporter
    {
        private const int CastShown = 5;
        private static readonly Regex ImagePath = new(@"^/[\w.-]{1,90}$", RegexOptions.Compiled);
        private readonly ApplicationDbContext _context;
        private readonly ITmdbClient _tmdb;

        public MovieImporter(ApplicationDbContext context, ITmdbClient tmdb)
        {
            _context = context;
            _tmdb = tmdb;
        }

        // Returns null when TMDB lacks what the catalogue needs, such as a release year or runtime.
        public async Task<Movie?> ImportAsync(int tmdbId)
        {
            var existing = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
            if (existing != null)
            {
                return existing;
            }

            var source = await _tmdb.GetMovieAsync(tmdbId);
            if (source == null || source.Adult || source.Softcore || string.IsNullOrWhiteSpace(source.Title)
                || source.Runtime is null or < 1 or > 1440 || !TryGetYear(source.ReleaseDate, out var year))
            {
                return null;
            }

            var movie = new Movie
            {
                TmdbId = source.Id,
                MovieTitle = Limit(source.Title, 200),
                Director = Limit(source.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown", 150),
                ReleaseYear = year,
                Duration = source.Runtime.Value,
                Overview = Limit(source.Overview, 2000),
                TopCast = Limit(string.Join(", ", (source.Credits?.Cast ?? new()).Take(CastShown).Select(c => c.Name)), 300),
                PosterPath = CleanPath(source.PosterPath),
                BackdropPath = CleanPath(source.BackdropPath),
                Genre = await FindGenreAsync(source.Genres?.FirstOrDefault()?.Name ?? "Other")
            };
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            return movie;
        }

        public async Task<int> ImportPopularAsync(int count)
        {
            var added = 0;
            for (var page = 1; added < count && page <= 3; page++)
            {
                foreach (var item in await _tmdb.PopularAsync(page))
                {
                    if (added == count)
                    {
                        break;
                    }
                    if (await ImportAsync(item.Id) != null)
                    {
                        added++;
                    }
                }
            }
            return added;
        }

        private async Task<Genre> FindGenreAsync(string name)
        {
            var clean = Limit(name, 100)!;
            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == clean);
            if (genre != null)
            {
                return genre;
            }
            genre = new Genre { Name = clean };
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();
            return genre;
        }

        private static bool TryGetYear(string? date, out int year)
        {
            year = 0;
            return date?.Length >= 4 && int.TryParse(date[..4], out year)
                && year >= 1888 && year <= DateTime.UtcNow.Year + 2;
        }

        private static string? CleanPath(string? path) =>
            path != null && ImagePath.IsMatch(path) ? path : null;

        private static string? Limit(string? text, int length) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Length <= length ? text : text[..length];
    }
}
