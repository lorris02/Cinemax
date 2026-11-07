using Cinemax.Data;
using Cinemax.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cinemax.Services
{
    public record TmdbHit(int TmdbId, string Title, string? Year, string? PosterUrl);

    public class MovieSearch
    {
        private const int MaxQuery = 100;
        private readonly ApplicationDbContext _context;
        private readonly ITmdbClient _tmdb;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MovieSearch> _logger;

        public MovieSearch(ApplicationDbContext context, ITmdbClient tmdb, IMemoryCache cache, ILogger<MovieSearch> logger)
        {
            _context = context;
            _tmdb = tmdb;
            _cache = cache;
            _logger = logger;
        }

        // Every word must appear in the title, director or cast, in any order.
        public static IQueryable<Movie> Matching(IQueryable<Movie> movies, string? q)
        {
            foreach (var word in (q ?? "").ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(8))
            {
                movies = movies.Where(m => m.MovieTitle!.ToLower().Contains(word)
                    || m.Director!.ToLower().Contains(word)
                    || (m.TopCast ?? "").ToLower().Contains(word));
            }
            return movies;
        }

        public List<Movie> Local(string q, int count)
        {
            var start = q.Trim().ToLower();
            return Matching(_context.Movies, q)
                .OrderByDescending(m => m.MovieTitle!.ToLower().StartsWith(start))
                .ThenBy(m => m.MovieTitle)
                .Take(count)
                .ToList();
        }

        // TMDB covers movies the catalogue does not hold yet. A TMDB outage only hides these extras.
        public async Task<List<TmdbHit>> FromTmdbAsync(string q, int count)
        {
            if (!_tmdb.Enabled)
            {
                return new();
            }
            var term = q.Trim().ToLower();
            term = term.Length > MaxQuery ? term[..MaxQuery] : term;
            try
            {
                var found = await _cache.GetOrCreateAsync($"tmdb-search:{term}", entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                    entry.Size = 1;
                    return _tmdb.SearchAsync(term);
                });
                var ids = (found ?? Array.Empty<TmdbMovieSummary>()).Select(m => m.Id).ToList();
                var known = await _context.Movies.Where(m => m.TmdbId != null && ids.Contains(m.TmdbId.Value))
                    .Select(m => m.TmdbId!.Value).ToListAsync();
                return (found ?? Array.Empty<TmdbMovieSummary>())
                    .Where(m => !known.Contains(m.Id) && !string.IsNullOrWhiteSpace(m.Title) && m.ReleaseDate?.Length >= 4)
                    .Take(count)
                    .Select(m => new TmdbHit(m.Id, m.Title!, m.ReleaseDate?.Length >= 4 ? m.ReleaseDate[..4] : null,
                        new Movie { PosterPath = MovieImporter.CleanPath(m.PosterPath) }.PosterUrl))
                    .ToList();
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("TMDB search failed: {Reason}", e.GetType().Name);
                return new();
            }
        }
    }
}
