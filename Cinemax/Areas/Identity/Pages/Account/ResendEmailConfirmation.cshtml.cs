using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Cinemax.Areas.Identity.Pages.Account;

public class ResendEmailConfirmationModel(UserManager<IdentityUser> userManager, IEmailSender emailSender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public bool Sent { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        // The page looks the same whether or not the email has an account.
        Sent = true;
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || await userManager.IsEmailConfirmedAsync(user)) return Page();
        await SendAsync(userManager, emailSender, Url, Request.Scheme, user);
        return Page();
    }

    public static async Task SendAsync(UserManager<IdentityUser> users, IEmailSender sender, IUrlHelper url, string scheme, IdentityUser user)
    {
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
        var link = url.Page("/Account/ConfirmEmail", pageHandler: null,
            values: new { area = "Identity", userId = user.Id, code }, protocol: scheme)!;
        await sender.SendEmailAsync(user.Email!, "Confirm your Cinemax email",
            $"<p>Confirm your email to start rating and reviewing movies.</p><p><a href=\"{HtmlEncoder.Default.Encode(link)}\" style=\"display:inline-block;background:#ff5a36;color:#190d09;padding:12px 18px;border-radius:9px;text-decoration:none;font-weight:700\">Confirm email</a></p>");
    }
}
