using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Cinemax.Services;

// Sends account emails through Resend's HTTPS API. Without a key, the message is logged so local runs still work.
public class ResendEmailSender(HttpClient http, IConfiguration configuration, ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var key = configuration["Email:ResendApiKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            logger.LogWarning("No Email:ResendApiKey set. Email to {Email}: {Subject} {Body}", email, subject, htmlMessage);
            return;
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = configuration["Email:From"] ?? "Cinemax <noreply@jonathanilori.xyz>",
                to = new[] { email },
                subject,
                html = Wrap(subject, htmlMessage),
            })
        };
        request.Headers.Authorization = new("Bearer", key);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            logger.LogError("Email to {Email} failed with status {Status}", email, (int)response.StatusCode);
    }

    // The Identity pages supply a short HTML body with the link; this adds the Cinemax look around it.
    public static string Wrap(string subject, string body) =>
        $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:28px;background:#0b0b0f;color:#f6f4f0;border-radius:12px">
        <p style="font-size:20px;font-weight:700;margin:0 0 18px;color:#ff5a36">Cinemax</p>
        <h1 style="font-size:22px;margin:0 0 12px">{HtmlEncoder.Default.Encode(subject)}</h1>
        <div style="line-height:1.6;color:#d8d6de">{body}</div>
        <p style="font-size:13px;color:#a7a5b0;margin-top:24px">If you did not ask for this, you can ignore this email.</p>
        </div>
        """;
}
