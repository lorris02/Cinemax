using System.Net;
using System.Text;
using Cinemax.Data;
using Cinemax.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CinemaxTest;

public class StubHandler : HttpMessageHandler
{
    private readonly HttpStatusCode status;
    private readonly string body;
    public Uri? LastUri { get; private set; }
    public StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK) { this.body = body; this.status = status; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        LastUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}

public class FakeTmdb : ITmdbClient
{
    public Dictionary<int, TmdbMovie> Movies { get; } = new();
    public bool Enabled => true;
    public Task<IReadOnlyList<TmdbMovieSummary>> PopularAsync(int page = 1) =>
        Task.FromResult<IReadOnlyList<TmdbMovieSummary>>(page > 1 ? new List<TmdbMovieSummary>() :
            Movies.Keys.Select(id => new TmdbMovieSummary(id, "Movie", null, null, null, false, false)).ToList());
    public Task<IReadOnlyList<TmdbMovieSummary>> SearchAsync(string query) => PopularAsync();
    public Task<TmdbMovie?> GetMovieAsync(int id) => Task.FromResult(Movies.GetValueOrDefault(id));
}

[TestClass]
public class TmdbTests
{
    private const string Key = "unit-test-key";

    private static TmdbClient Client(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://tmdb.test/3/") },
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Tmdb:ApiKey"] = Key }).Build());

    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TmdbMovie Sample(int id = 7, int? runtime = 120, bool adult = false) => new(id, "Sample", "A plot", "/poster.jpg", "/back.jpg",
        "2024-05-01", runtime, adult, false, new() { new("Drama") },
        new(new() { new("Ann"), new("Ben") }, new() { new("Cam", "Writer"), new("Dee", "Director") }));

    [TestMethod]
    public async Task PopularHidesAdultResults()
    {
        var handler = new StubHandler("""{"results":[{"id":1,"title":"Safe"},{"id":2,"title":"Adult","adult":true},{"id":3,"title":"Soft","softcore":true}]}""");
        var movies = await Client(handler).PopularAsync();
        Assert.AreEqual(1, movies.Count);
        Assert.AreEqual("Safe", movies[0].Title);
        StringAssert.Contains(handler.LastUri!.Query, "api_key=" + Key);
    }

    [TestMethod]
    public async Task ErrorsDoNotRevealTheApiKey()
    {
        var error = await Assert.ThrowsExceptionAsync<HttpRequestException>(() =>
            Client(new StubHandler("{}", HttpStatusCode.Unauthorized)).PopularAsync());
        Assert.IsFalse(error.Message.Contains(Key));
    }

    [TestMethod]
    public async Task MissingMovieReturnsNull()
    {
        Assert.IsNull(await Client(new StubHandler("{}", HttpStatusCode.NotFound)).GetMovieAsync(1));
    }

    [TestMethod]
    public async Task ImportSavesDetailsAndCreatesGenre()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        tmdb.Movies[7] = Sample();
        var movie = await new MovieImporter(db, tmdb).ImportAsync(7);
        Assert.IsNotNull(movie);
        Assert.AreEqual("Dee", movie.Director);
        Assert.AreEqual("Ann, Ben", movie.TopCast);
        Assert.AreEqual(2024, movie.ReleaseYear);
        Assert.AreEqual("Drama", db.Genres.Single().Name);
        Assert.AreEqual("https://image.tmdb.org/t/p/w500/poster.jpg", movie.PosterUrl);
    }

    [TestMethod]
    public async Task ImportDoesNotDuplicateAMovie()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        tmdb.Movies[7] = Sample();
        var importer = new MovieImporter(db, tmdb);
        await importer.ImportAsync(7);
        await importer.ImportAsync(7);
        Assert.AreEqual(1, db.Movies.Count());
    }

    [TestMethod]
    public async Task ImportSkipsIncompleteAndAdultMovies()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        tmdb.Movies[1] = Sample(1, runtime: 0);
        tmdb.Movies[2] = Sample(2, adult: true);
        var importer = new MovieImporter(db, tmdb);
        Assert.IsNull(await importer.ImportAsync(1));
        Assert.IsNull(await importer.ImportAsync(2));
        Assert.AreEqual(0, db.Movies.Count());
    }

    [TestMethod]
    public async Task ImportRejectsUnexpectedImagePaths()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        tmdb.Movies[7] = Sample() with { PosterPath = "/../evil?x=1" };
        var movie = await new MovieImporter(db, tmdb).ImportAsync(7);
        Assert.IsNull(movie!.PosterPath);
    }

    [TestMethod]
    public async Task PopularImportStopsAtTheRequestedCount()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        foreach (var id in new[] { 1, 2, 3 }) tmdb.Movies[id] = Sample(id);
        Assert.AreEqual(2, await new MovieImporter(db, tmdb).ImportPopularAsync(2));
        Assert.AreEqual(2, db.Movies.Count());
    }

    [TestMethod]
    public async Task PopularImportDoesNotCountMoviesAlreadyInTheCatalogue()
    {
        using var db = Database();
        var tmdb = new FakeTmdb();
        foreach (var id in new[] { 1, 2, 3 }) tmdb.Movies[id] = Sample(id);
        var importer = new MovieImporter(db, tmdb);
        await importer.ImportAsync(1);
        Assert.AreEqual(2, await importer.ImportPopularAsync(2));
        Assert.AreEqual(3, db.Movies.Count());
    }
}
