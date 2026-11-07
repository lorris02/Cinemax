using System.Text.RegularExpressions;
using Cinemax.Data;
using Cinemax.Models;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Services
{
    public class MovieImporter
    {
        private const int CastShown = 5;
        private const int ReviewsShown = 4;
        private const int ReviewLength = 900;
        private static readonly Regex Markup = new(@"<[^>]+>|\*\*|__|(?<!\w)_|_(?!\w)", RegexOptions.Compiled);
        private static readonly Regex Offensive = new(@"\b(fuck|shit|bitch|asshole|cunt|faggot|retard)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ImagePath = new(@"^/[\w.-]{1,90}$", RegexOptions.Compiled);
        private readonly ApplicationDbContext _context;
        private readonly ITmdbClient _tmdb;

        public MovieImporter(ApplicationDbContext context, ITmdbClient tmdb)
        {
            _context = context;
            _tmdb = tmdb;
        }

        public Task<Movie?> FindAsync(int tmdbId) =>
            _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);

        public Task<Dictionary<int, int>> KnownAsync(IEnumerable<int> tmdbIds)
        {
            var ids = tmdbIds.ToList();
            return _context.Movies.Where(m => m.TmdbId != null && ids.Contains(m.TmdbId.Value))
                .ToDictionaryAsync(m => m.TmdbId!.Value, m => m.MovieId);
        }

        // Returns null when TMDB lacks what the catalogue needs, such as a title or release year.
        public async Task<Movie?> ImportAsync(int tmdbId)
        {
            var existing = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
            if (existing != null)
            {
                return existing;
            }

            var source = await _tmdb.GetMovieAsync(tmdbId);
            if (source == null || source.Adult || source.Softcore || string.IsNullOrWhiteSpace(source.Title)
                || source.Runtime is < 0 or > 1440 || !TryGetYear(source.ReleaseDate, out var year))
            {
                return null;
            }

            var movie = new Movie
            {
                TmdbId = source.Id,
                MovieTitle = Limit(source.Title, 200),
                Director = Limit(source.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown", 150),
                ReleaseYear = year,
                Duration = source.Runtime ?? 0,
                Overview = Limit(source.Overview, 2000),
                TopCast = Limit(string.Join(", ", (source.Credits?.Cast ?? new()).Take(CastShown).Select(c => c.Name)), 300),
                PosterPath = CleanPath(source.PosterPath),
                BackdropPath = CleanPath(source.BackdropPath),
                Genre = await FindGenreAsync(source.Genres?.FirstOrDefault()?.Name ?? "Other")
            };
            ApplyDetails(movie, source);
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
                    if (!await _context.Movies.AnyAsync(m => m.TmdbId == item.Id) && await ImportAsync(item.Id) != null)
                    {
                        added++;
                    }
                }
            }
            return added;
        }

        // Mixes popular releases with classics until the catalogue reaches the target.
        public async Task GrowCatalogueAsync(int target, int maxPages, TimeSpan pause, CancellationToken stop)
        {
            for (var page = 1; page <= maxPages; page++)
            {
                var lists = new[] { await _tmdb.PopularAsync(page), await _tmdb.TopRatedAsync(page) };
                foreach (var item in lists.SelectMany(list => list))
                {
                    stop.ThrowIfCancellationRequested();
                    if (await _context.Movies.CountAsync(stop) >= target)
                    {
                        return;
                    }
                    if (!await _context.Movies.AnyAsync(m => m.TmdbId == item.Id, stop))
                    {
                        await ImportAsync(item.Id);
                        await Task.Delay(pause, stop);
                    }
                }
            }
        }

        // Adds the rating and viewer reviews to movies imported before they were collected.
        public async Task RefreshDetailsAsync(TimeSpan pause, CancellationToken stop)
        {
            var movies = await _context.Movies.Where(m => m.TmdbId != null && !m.TmdbDetailsLoaded).ToListAsync(stop);
            foreach (var movie in movies)
            {
                var source = await _tmdb.GetMovieAsync(movie.TmdbId!.Value);
                if (source != null)
                {
                    ApplyDetails(movie, source);
                    await _context.SaveChangesAsync(stop);
                }
                await Task.Delay(pause, stop);
            }
        }

        private void ApplyDetails(Movie movie, TmdbMovie source)
        {
            movie.TmdbRating = source.VoteAverage > 0 ? Math.Round(source.VoteAverage.Value, 1) : null;
            movie.TmdbDetailsLoaded = true;
            foreach (var item in (source.Reviews?.Results ?? new()).Where(Usable).Take(ReviewsShown))
            {
                movie.Reviews.Add(new Review
                {
                    TmdbReviewId = item.Id,
                    ReviewerName = Limit(item.Author, 100),
                    ReviewMessage = ShortReview(item.Content!),
                    Rating = Math.Clamp((int)Math.Round(item.AuthorDetails!.Rating!.Value / 2, MidpointRounding.AwayFromZero), 1, 5),
                    SourceUrl = item.Url?.StartsWith("https://www.themoviedb.org/") == true ? Limit(item.Url, 200) : null
                });
            }
        }

        private static bool Usable(TmdbReview review) =>
            review.AuthorDetails?.Rating > 0 && !string.IsNullOrWhiteSpace(review.Author)
            && !string.IsNullOrWhiteSpace(review.Content) && review.Id?.Length is > 0 and <= 50
            && !Offensive.IsMatch(review.Content);

        // TMDB reviews can hold markup, and Cinemax shows a shortened version with credit.
        public static string ShortReview(string content)
        {
            var clean = Markup.Replace(content, "").Replace("\r", "").Trim();
            if (clean.Length <= ReviewLength)
            {
                return clean;
            }
            var cut = clean.LastIndexOf(' ', ReviewLength);
            return clean[..(cut > 0 ? cut : ReviewLength)].TrimEnd() + "...";
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

        public static string? CleanPath(string? path) =>
            path != null && ImagePath.IsMatch(path) ? path : null;

        private static string? Limit(string? text, int length) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Length <= length ? text : text[..length];
    }
}
