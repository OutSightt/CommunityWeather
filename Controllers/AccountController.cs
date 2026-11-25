using System.Security.Claims;
using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityWeather.Controllers;

public class AccountController : Controller
{
    private readonly AccountService _accounts;
    private readonly AppDbContext _db;

    public AccountController(AccountService accounts, AppDbContext db)
    {
        _accounts = accounts;
        _db = db;
    }

    // ---------- Registration ----------

    [AllowAnonymous, HttpGet]
    public IActionResult Register() =>
        User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accounts.RegisterAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Email), result.Error!);
            return View(model);
        }

        await SignInAsync(result.User!, persistent: false);
        TempData["Message"] = "Your account is ready. Welcome to Community Weather.";
        return RedirectToDashboard();
    }

    // ---------- Login and logout ----------

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accounts.AuthenticateAsync(model.Email, model.Password);
        switch (result.Status)
        {
            case LoginStatus.Success:
                await SignInAsync(result.User!, model.RememberMe);
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                return RedirectToDashboard();

            case LoginStatus.LockedOut:
                var minutes = Math.Max(1, (int)Math.Ceiling(result.RetryAfter?.TotalMinutes ?? 1));
                ModelState.AddModelError("", $"Too many failed attempts. Try again in {minutes} minute(s).");
                break;

            case LoginStatus.Disabled:
                ModelState.AddModelError("", "This account has been disabled. Contact a community administrator.");
                break;

            default:
                // One message for both an unknown email and a wrong password, so emails cannot be guessed.
                ModelState.AddModelError("", "Invalid email or password.");
                break;
        }

        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Message"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Role-based landing ----------

    /// <summary>Sends the signed-in user to the dashboard that matches their role.</summary>
    [Authorize, HttpGet]
    public IActionResult Dashboard() => RedirectToDashboard();

    public IActionResult AccessDenied() => View();

    // ---------- Profile ----------

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Login));
        return View(new ProfileViewModel { FullName = user.FullName, Email = user.Email, EmailAlerts = user.EmailAlerts });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await CurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Login));
        model.Email = user.Email;
        if (!ModelState.IsValid) return View(model);

        user.FullName = model.FullName.Trim();
        user.EmailAlerts = model.EmailAlerts;
        await _db.SaveChangesAsync();
        await SignInAsync(user, await IsPersistentAsync()); // refresh the display name in the cookie

        TempData["Message"] = "Profile saved.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await CurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Login));

        if (!await _accounts.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Your current password is not correct.");
            return View(model);
        }

        TempData["Message"] = "Password changed.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Helpers ----------

    private IActionResult RedirectToDashboard() =>
        User.IsInRole(Roles.Admin)
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Resident");

    private async Task<User?> CurrentUserAsync()
    {
        var idText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idText, out var id) ? await _db.Users.FindAsync(id) : null;
    }

    private async Task<bool> IsPersistentAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return auth.Properties?.IsPersistent ?? false;
    }

    private async Task SignInAsync(User user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = persistent });
    }
}
