using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cinemax.Areas.Identity.Pages.Account;

public class LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];
    public string? ReturnUrl { get; set; }
    public bool NeedsConfirmation { get; set; }
    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(returnUrl);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Too many attempts. Try again in a few minutes.");
            return Page();
        }
        // Identity says "not allowed" before it checks the password, so the password is checked here
        // to avoid telling a stranger that an unconfirmed account exists.
        if (result.IsNotAllowed && await userManager.FindByEmailAsync(Input.Email) is { } user
            && await userManager.CheckPasswordAsync(user, Input.Password))
        {
            NeedsConfirmation = true;
            ModelState.AddModelError(string.Empty, "Confirm your email first. We sent you a link when you signed up.");
            return Page();
        }
        ModelState.AddModelError(string.Empty, "Incorrect email or password.");
        return Page();
    }
}
