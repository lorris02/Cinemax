using Cinemax.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Data
{
    public static class DatabaseSetup
    {
        public const string AdminRole = "Administrator";
        private const int StarterMovies = 24;

        public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var db = provider.GetRequiredService<ApplicationDbContext>();
            if (db.Database.IsRelational())
            {
                await db.Database.MigrateAsync();
            }

            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseSetup));
            await CreateAdminAsync(provider, configuration, logger);
            await FillCatalogueAsync(provider, db, logger);
        }

        // A TMDB outage should never stop the site from starting.
        private static async Task FillCatalogueAsync(IServiceProvider provider, ApplicationDbContext db, ILogger logger)
        {
            if (!provider.GetRequiredService<ITmdbClient>().Enabled || await db.Movies.AnyAsync())
            {
                return;
            }
            try
            {
                var added = await provider.GetRequiredService<MovieImporter>().ImportPopularAsync(StarterMovies);
                logger.LogInformation("Added {Count} starter movies from TMDB.", added);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning("Could not add starter movies from TMDB: {Reason}", e.GetType().Name);
            }
        }

        // Only an account created here becomes admin, so nobody can claim the role by signing up with the admin email.
        private static async Task CreateAdminAsync(IServiceProvider provider, IConfiguration configuration, ILogger logger)
        {
            var email = configuration["Admin:Email"];
            var password = configuration["Admin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(AdminRole))
            {
                await roles.CreateAsync(new IdentityRole(AdminRole));
            }

            var users = provider.GetRequiredService<UserManager<IdentityUser>>();
            if (await users.FindByEmailAsync(email) is not null)
            {
                logger.LogInformation("Admin setup skipped because that account already exists.");
                return;
            }

            var admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await users.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                logger.LogError("Admin setup failed: {Errors}", string.Join(" ", created.Errors.Select(e => e.Description)));
                return;
            }
            await users.AddToRoleAsync(admin, AdminRole);
        }
    }
}
