using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CinemaxTest;

// Keeps emails in memory instead of sending them.
public class CapturedEmails : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Sent.Add((email, subject, htmlMessage));
        return Task.CompletedTask;
    }
    public string LastLink(string email) => WebUtility.HtmlDecode(
        Regex.Match(Sent.Last(m => m.To == email).Body, "href=[\"']([^\"']+)[\"']").Groups[1].Value);
}

[TestClass]
public class EmailFlowTests
{
    private CinemaxFactory factory = null!;
    private CapturedEmails mail = null!;
    private HttpClient client = null!;
    private const string Password = "Long-password-1";

    [TestInitialize]
    public void Setup()
    {
        mail = new CapturedEmails();
        factory = new CinemaxFactory();
        var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(mail);
        }));
        client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    }

    [TestCleanup]
    public void Cleanup() { client.Dispose(); factory.Dispose(); }

    private async Task<HttpResponseMessage> Post(string page, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(page);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(
            Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        return await client.PostAsync(page, new FormUrlEncodedContent(fields));
    }

    private Task<HttpResponseMessage> Register(string email) => Post("/Identity/Account/Register",
        new() { ["Input.Email"] = email, ["Input.Password"] = Password, ["Input.ConfirmPassword"] = Password });

    private Task<HttpResponseMessage> Login(string email, string password) => Post("/Identity/Account/Login",
        new() { ["Input.Email"] = email, ["Input.Password"] = password });

    [TestMethod]
    public async Task NewAccountsConfirmEmailBeforeLoggingIn()
    {
        var registered = await Register("fan@example.com");
        Assert.AreEqual(HttpStatusCode.Redirect, registered.StatusCode);
        StringAssert.Contains(registered.Headers.Location!.ToString(), "RegisterConfirmation");
        Assert.AreEqual(1, mail.Sent.Count);

        var early = await Login("fan@example.com", Password);
        Assert.AreEqual(HttpStatusCode.OK, early.StatusCode);
        StringAssert.Contains(await early.Content.ReadAsStringAsync(), "Confirm your email first");
        // A wrong password gets the usual message, so it does not reveal the unconfirmed account.
        var wrong = await Login("fan@example.com", "Wrong-password-9");
        StringAssert.Contains(await wrong.Content.ReadAsStringAsync(), "Incorrect email or password");

        var link = new Uri(mail.LastLink("fan@example.com"));
        var confirmed = await client.GetAsync(link.PathAndQuery);
        Assert.AreEqual(HttpStatusCode.Redirect, confirmed.StatusCode);
        var bogus = await client.GetAsync(link.AbsolutePath + "?userId=x&code=bad");
        StringAssert.Contains(await bogus.Content.ReadAsStringAsync(), "Link expired");

        var login = await Login("fan@example.com", Password);
        Assert.AreEqual(HttpStatusCode.Redirect, login.StatusCode);
    }

    [TestMethod]
    public async Task ResendOnlyMailsUnconfirmedAccounts()
    {
        await Register("again@example.com");
        await Post("/Identity/Account/ResendEmailConfirmation", new() { ["Input.Email"] = "again@example.com" });
        await Post("/Identity/Account/ResendEmailConfirmation", new() { ["Input.Email"] = "nobody@example.com" });
        Assert.AreEqual(2, mail.Sent.Count(m => m.To == "again@example.com"));
        Assert.IsFalse(mail.Sent.Any(m => m.To == "nobody@example.com"));
    }

    [TestMethod]
    public async Task ForgotPasswordResetsIt()
    {
        await Register("reset@example.com");
        await client.GetAsync(new Uri(mail.LastLink("reset@example.com")).PathAndQuery);
        await Post("/Identity/Account/ForgotPassword", new() { ["Input.Email"] = "reset@example.com" });
        var resetLink = new Uri(mail.LastLink("reset@example.com"));
        StringAssert.Contains(resetLink.AbsolutePath, "ResetPassword");
        var html = await client.GetStringAsync(resetLink.PathAndQuery);
        var code = WebUtility.HtmlDecode(Regex.Match(html, "id=\"Input_Code\"[^>]*value=\"([^\"]+)\"|value=\"([^\"]+)\"[^>]*id=\"Input_Code\"").Groups.Values.Skip(1).First(g => g.Success).Value);
        var reset = await Post(resetLink.PathAndQuery, new()
        {
            ["Input.Code"] = code, ["Input.Email"] = "reset@example.com",
            ["Input.Password"] = "Newer-password-2", ["Input.ConfirmPassword"] = "Newer-password-2"
        });
        Assert.AreEqual(HttpStatusCode.Redirect, reset.StatusCode);
        Assert.AreEqual(HttpStatusCode.Redirect, (await Login("reset@example.com", "Newer-password-2")).StatusCode);
    }

    [TestMethod]
    public async Task EmailPagesAreRateLimited()
    {
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 7; i++)
            statuses.Add((await Post("/Identity/Account/ForgotPassword", new() { ["Input.Email"] = "x@example.com" })).StatusCode);
        CollectionAssert.Contains(statuses, HttpStatusCode.TooManyRequests);
    }
}
