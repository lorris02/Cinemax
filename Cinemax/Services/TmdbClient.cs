using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cinemax.Services
{
    public record TmdbMovieSummary(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("adult")] bool Adult,
        [property: JsonPropertyName("softcore")] bool Softcore);

    public record TmdbPage([property: JsonPropertyName("results")] List<TmdbMovieSummary>? Results);
    public record TmdbGenre([property: JsonPropertyName("name")] string? Name);
    public record TmdbCastMember([property: JsonPropertyName("name")] string? Name);
    public record TmdbCrewMember(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("job")] string? Job);
    public record TmdbCredits(
        [property: JsonPropertyName("cast")] List<TmdbCastMember>? Cast,
        [property: JsonPropertyName("crew")] List<TmdbCrewMember>? Crew);

    public record TmdbMovie(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("overview")] string? Overview,
        [property: JsonPropertyName("poster_path")] string? PosterPath,
        [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("runtime")] int? Runtime,
        [property: JsonPropertyName("adult")] bool Adult,
        [property: JsonPropertyName("softcore")] bool Softcore,
        [property: JsonPropertyName("genres")] List<TmdbGenre>? Genres,
        [property: JsonPropertyName("credits")] TmdbCredits? Credits);

    public interface ITmdbClient
    {
        bool Enabled { get; }
        Task<IReadOnlyList<TmdbMovieSummary>> PopularAsync(int page = 1);
        Task<IReadOnlyList<TmdbMovieSummary>> SearchAsync(string query);
        Task<TmdbMovie?> GetMovieAsync(int id);
    }

    public class TmdbClient : ITmdbClient
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;

        public TmdbClient(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _apiKey = configuration["Tmdb:ApiKey"];
        }

        public bool Enabled => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<IReadOnlyList<TmdbMovieSummary>> PopularAsync(int page = 1) =>
            Safe(await GetAsync<TmdbPage>($"movie/popular?page={page}"));

        public async Task<IReadOnlyList<TmdbMovieSummary>> SearchAsync(string query) =>
            Safe(await GetAsync<TmdbPage>($"search/movie?include_adult=false&query={Uri.EscapeDataString(query)}"));

        public Task<TmdbMovie?> GetMovieAsync(int id) =>
            GetAsync<TmdbMovie>($"movie/{id}?append_to_response=credits");

        private static List<TmdbMovieSummary> Safe(TmdbPage? page) =>
            (page?.Results ?? new()).Where(m => !m.Adult && !m.Softcore).ToList();

        private async Task<T?> GetAsync<T>(string path) where T : class
        {
            if (!Enabled)
            {
                return null;
            }
            var separator = path.Contains('?') ? '&' : '?';
            var url = $"{path}{separator}language=en-US&api_key={Uri.EscapeDataString(_apiKey!)}";
            using var response = await _http.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                // The message stays free of the URL because the URL holds the API key.
                throw new HttpRequestException($"TMDB returned {(int)response.StatusCode}.");
            }
            await using var body = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(body);
        }
    }
}
