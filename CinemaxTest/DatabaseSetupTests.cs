using Cinemax.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaxTest;

[TestClass]
public class DatabaseSetupTests
{
    private const string Email = "owner@example.com";
    private static readonly string Password = $"Aa1!{Guid.NewGuid()}";

    private static IConfiguration Config(string? email, string? password) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Admin:Email"] = email, ["Admin:Password"] = password
        }).Build();

    [TestMethod]
    public async Task CreatesAdminFromPrivateSettings()
    {
        using var factory = new CinemaxFactory();
        await DatabaseSetup.RunAsync(factory.Services, Config(Email, Password));

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await users.FindByEmailAsync(Email);
        Assert.IsNotNull(admin);
        Assert.IsTrue(await users.IsInRoleAsync(admin, DatabaseSetup.AdminRole));
    }

    [TestMethod]
    public async Task DoesNotPromoteAccountThatAlreadyUsesAdminEmail()
    {
        using var factory = new CinemaxFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            await users.CreateAsync(new IdentityUser { UserName = Email, Email = Email }, Password);
        }

        await DatabaseSetup.RunAsync(factory.Services, Config(Email, Password));

        using var check = factory.Services.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await manager.FindByEmailAsync(Email);
        Assert.IsFalse(await manager.IsInRoleAsync(user!, DatabaseSetup.AdminRole));
    }

    [TestMethod]
    public async Task DoesNothingWithoutAdminSettings()
    {
        using var factory = new CinemaxFactory();
        await DatabaseSetup.RunAsync(factory.Services, Config(null, null));

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.AreEqual(0, users.Users.Count());
    }
}
