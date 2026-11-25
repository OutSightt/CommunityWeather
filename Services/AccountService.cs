using CommunityWeather.Data;
using CommunityWeather.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Services;

public enum LoginStatus { Success, InvalidCredentials, LockedOut, Disabled }

public record LoginResult(LoginStatus Status, User? User = null, TimeSpan? RetryAfter = null);

public record RegisterResult(bool Succeeded, User? User = null, string? Error = null);

/// <summary>Account business rules: registration, authentication, lockout and password changes.</summary>
public class AccountService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);

    // Verified when the email is unknown so that response time does not reveal which emails exist.
    private static readonly string DummyHash = PasswordHelper.Hash("not-a-real-password");

    private const string DuplicateMessage = "An account with this email already exists. Try logging in instead.";

    private readonly AppDbContext _db;

    public AccountService(AppDbContext db) => _db = db;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Creates a Resident account. Administrators are never created through public registration.</summary>
    public async Task<RegisterResult> RegisterAsync(RegisterViewModel model)
    {
        var email = NormalizeEmail(model.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return new RegisterResult(false, Error: DuplicateMessage);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(model.Password),
            Role = Roles.Resident,
            EmailAlerts = model.EmailAlerts
        };
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two people registering the same email at once: the unique index rejects the second one.
            _db.Entry(user).State = EntityState.Detached;
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return new RegisterResult(false, Error: DuplicateMessage);
            throw;
        }

        return new RegisterResult(true, user);
    }

    public async Task<LoginResult> AuthenticateAsync(string email, string password)
    {
        var normalized = NormalizeEmail(email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        if (user == null)
        {
            PasswordHelper.Verify(password, DummyHash);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
            return new LoginResult(LoginStatus.LockedOut, RetryAfter: user.LockoutEnd.Value - now);

        if (!PasswordHelper.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = now + LockoutDuration;
                await _db.SaveChangesAsync();
                return new LoginResult(LoginStatus.LockedOut, RetryAfter: LockoutDuration);
            }
            await _db.SaveChangesAsync();
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        // Only tell the person their account is disabled once they proved they own it.
        if (!user.IsActive)
            return new LoginResult(LoginStatus.Disabled);

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return new LoginResult(LoginStatus.Success, user);
    }

    public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        if (!PasswordHelper.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = PasswordHelper.Hash(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }
}
