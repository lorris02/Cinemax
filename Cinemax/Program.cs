using Cinemax.Data;
using System.Threading.RateLimiting;
using Cinemax.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()// enables role management
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// The client logger is removed so the API key in request URLs never reaches the logs.
builder.Services.AddHttpClient<ITmdbClient, TmdbClient>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(15);
}).RemoveAllLoggers();
builder.Services.AddScoped<MovieImporter>();
builder.Services.AddScoped<MovieSearch>();
builder.Services.AddMemoryCache(options => options.SizeLimit = 500);
builder.Services.AddHostedService<CatalogueSync>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("search", context => PerAddress(context, 90));
    options.AddPolicy("import", context => PerAddress(context, 15));
});

// Optional providers are enabled only when both credentials are configured.
var authentication = builder.Services.AddAuthentication();
var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
{
    authentication.AddGoogle(options => { options.ClientId = googleId; options.ClientSecret = googleSecret; });
}
var githubId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(githubId) && !string.IsNullOrWhiteSpace(githubSecret))
{
    authentication.AddGitHub(options => { options.ClientId = githubId; options.ClientSecret = githubSecret; });
}
var app = builder.Build();

await DatabaseSetup.RunAsync(app.Services, app.Configuration);

// Nginx ends HTTPS and passes the original scheme and visitor address.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static RateLimitPartition<string> PerAddress(HttpContext context, int perMinute) =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1) });

public partial class Program { }
