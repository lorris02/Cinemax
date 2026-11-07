using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Cinemax.Data;
using Cinemax.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaxTest;

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Test-Role"].ToString();
        if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role)
        }, "Test");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
    }
}

public class CinemaxFactory : WebApplicationFactory<Program>
{
    private readonly string database = Guid.NewGuid().ToString();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=unused.db",
                ["Tmdb:ApiKey"] = ""
            }));
        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }
}

[TestClass]
public class RequestSecurityTests
{
    private CinemaxFactory factory = null!;
    private HttpClient client = null!;
    [TestInitialize]
    public void Setup()
    {
        factory = new CinemaxFactory();
        client = factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost")
        });
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Genres.Add(new Genre { GenreId = 1, Name = "Drama" });
        db.Movies.Add(new Movie { MovieId = 1, MovieTitle = "Original", Director = "Director", ReleaseYear = 2024, Duration = 100, GenreId = 1 });
        db.Reviews.Add(new Review { ReviewId = 1, ReviewerName = "Reviewer", ReviewMessage = "Original review", MovieId = 1, Rating = 4 });
        db.SaveChanges();
    }
    [TestCleanup]
    public void Cleanup() { client.Dispose(); factory.Dispose(); }

    [TestMethod]
    public async Task PublicListsAreAvailable()
    {
        foreach (var path in new[] { "/Movie", "/Review", "/Genre" })
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode, path);
    }
    [TestMethod]
    public async Task AnonymousVisitorsCannotOpenWriteForms()
    {
        foreach (var path in new[] { "/Movie/Create", "/Movie/Edit/1", "/Review/Create", "/Review/Edit/1", "/Genre/Create", "/Genre/Edit/1" })
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode, path);
    }
    [TestMethod]
    public async Task VisitorsCanOpenHomeAndMovieDetails()
    {
        foreach (var path in new[] { "/", "/Movie/Details/1", "/Movie?q=m", "/Movie?genreId=1" })
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode, path);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/Movie/Details/999")).StatusCode);
    }

    [TestMethod]
    public async Task AccountPagesHideSetupNotesAndUnusedEmailLinks()
    {
        foreach (var path in new[] { "/Identity/Account/Login", "/Identity/Account/Register" })
        {
            var page = await client.GetStringAsync(path);
            foreach (var hidden in new[] { "external authentication services", "ForgotPassword", "ResendEmailConfirmation" })
                Assert.IsFalse(page.Contains(hidden), $"{path} shows {hidden}");
        }
    }

    [TestMethod]
    public async Task MissingPagesShowTheNotFoundScreen()
    {
        var response = await client.GetAsync("/nope/nothing");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Page not found");
    }

    [TestMethod]
    public async Task SuggestReturnsMatchesAndIgnoresOneLetter()
    {
        var body = await client.GetStringAsync("/Search/Suggest?q=m");
        StringAssert.Contains(body, "\"local\":[]");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/Search/Suggest?q=movie")).StatusCode);
    }

    [TestMethod]
    public async Task OpeningATmdbMovieRequiresTheFormToken()
    {
        var response = await client.PostAsync("/Search/Open", new FormUrlEncodedContent(new Dictionary<string, string> { ["tmdbId"] = "1" }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task OnlyAdminCanOpenTmdbImport()
    {
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/MovieImport")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync("/MovieImport")).StatusCode);
    }

    [TestMethod]
    public async Task CustomerCannotModerateCatalogueOrReviews()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        foreach (var path in new[] { "/Movie/Create", "/Movie/Edit/1", "/Review/Edit/1", "/Genre/Create", "/Genre/Edit/1" })
            Assert.AreEqual(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode, path);
        foreach (var path in new[] { "/Movie/Create", "/Movie/Delete/1", "/Review/Delete/1", "/Genre/Delete/1", "/Movie/Edit/1", "/Review/Edit/1", "/Genre/Edit/1" })
            Assert.AreEqual(HttpStatusCode.Forbidden, (await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode, path);
    }
    [TestMethod]
    public async Task GetRequestsCannotDeleteRecords()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        foreach (var path in new[] { "/Movie/Delete/1", "/Review/Delete/1", "/Genre/Delete/1" })
        {
            var status = (await client.GetAsync(path)).StatusCode;
            Assert.IsTrue(status == HttpStatusCode.NotFound || status == HttpStatusCode.MethodNotAllowed, path);
        }
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.AreEqual(1, db.Movies.Count());
        Assert.AreEqual(1, db.Reviews.Count());
        Assert.AreEqual(1, db.Genres.Count());
    }
    [TestMethod]
    public async Task AdministratorDeletesRequireAntiforgery()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        foreach (var path in new[] { "/Movie/Delete/1", "/Review/Delete/1", "/Genre/Delete/1" })
            Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode, path);
    }
    private async Task<string> Token(string path)
    {
        var response = await client.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
        var html = await response.Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    [TestMethod]
    public async Task TamperedMovieIdDoesNotChangeAnotherRecord()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        var token = await Token("/Movie/Edit/1");
        var response = await client.PostAsync("/Movie/Edit/1", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = token, ["MovieId"] = "99", ["MovieTitle"] = "Tampered",
            ["Director"] = "Director", ["ReleaseYear"] = "2024", ["Duration"] = "100", ["GenreId"] = "1"
        }));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.AreEqual("Original", db.Movies.Find(1)!.MovieTitle);
        Assert.AreEqual(1, db.Movies.Count());
    }
    [TestMethod]
    public async Task ValidReviewDeleteRemovesOnlyTheReview()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        var token = await Token("/Review");
        var response = await client.PostAsync("/Review/Delete/1", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.AreEqual(0, db.Reviews.Count());
        Assert.AreEqual(1, db.Movies.Count());
    }
    [TestMethod]
    public async Task GenreUsedByMovieCannotBeDeleted()
    {
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        var token = await Token("/Genre");
        var response = await client.PostAsync("/Genre/Delete/1", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.AreEqual(1, scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Genres.Count());
    }
}
