using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Cinemax.Areas.Identity.Pages.Account;

public class ConfirmEmailModel(UserManager<IdentityUser> userManager) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId is null || code is null || await userManager.FindByIdAsync(userId) is not { } user) return Page();
        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return Page();
        }
        if (!(await userManager.ConfirmEmailAsync(user, token)).Succeeded) return Page();
        StatusMessage = "Email confirmed. You can log in now.";
        return RedirectToPage("./Login");
    }
}
