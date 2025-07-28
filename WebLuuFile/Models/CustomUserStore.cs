using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebLuuFile.Data;

public class CustomUserStore :
    IUserStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserEmailStore<IdentityUser>
{
    private readonly ApplicationDbContext _db;
    public CustomUserStore(ApplicationDbContext db) => _db = db;

    // ======= IUserStore =======
    public async Task<IdentityUser> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
        await _db.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);

    public async Task<IdentityUser> FindByIdAsync(string userId, CancellationToken ct) =>
        await _db.Users.FindAsync(new object[] { userId }, ct);

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.Id);

    public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(IdentityUser user, string name, CancellationToken _)
    {
        user.UserName = name; return Task.CompletedTask;
    }

    public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(IdentityUser user, string name, CancellationToken _)
    {
        user.NormalizedUserName = name; return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken ct)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken ct)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return IdentityResult.Success;
    }

    public void Dispose() { /* nothing to dispose */ }

    // ======= IUserPasswordStore =======
    public Task SetPasswordHashAsync(IdentityUser user, string hash, CancellationToken _)
    {
        user.PasswordHash = hash; return Task.CompletedTask;
    }
    public Task<string> GetPasswordHashAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.PasswordHash);
    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.PasswordHash != null);

    // ======= IUserEmailStore =======
    public Task SetEmailAsync(IdentityUser user, string email, CancellationToken _)
    {
        user.Email = email; return Task.CompletedTask;
    }
    public Task<string> GetEmailAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.Email);
    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.EmailConfirmed);
    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken _)
    {
        user.EmailConfirmed = confirmed; return Task.CompletedTask;
    }
    public async Task<IdentityUser> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        await _db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);
    public Task<string> GetNormalizedEmailAsync(IdentityUser user, CancellationToken _) =>
        Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(IdentityUser user, string normalizedEmail, CancellationToken _)
    {
        user.NormalizedEmail = normalizedEmail; return Task.CompletedTask;
    }
}
