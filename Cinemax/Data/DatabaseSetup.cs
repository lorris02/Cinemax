using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinemax.Data
{
    public static class DatabaseSetup
    {
        public const string AdminRole = "Administrator";

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
