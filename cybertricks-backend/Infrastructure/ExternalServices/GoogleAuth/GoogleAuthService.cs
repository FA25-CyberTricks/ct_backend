using ct.backend.Common.Ports.GoogleAuth;
using ct.backend.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ct.backend.Infrastructure.ExternalServices.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GoogleAuthService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync()
    {
        return await _signInManager.GetExternalLoginInfoAsync();
    }

    public async Task<SignInResult> ExternalLoginSignInAsync(string provider, string providerKey)
    {
        return await _signInManager.ExternalLoginSignInAsync(provider, providerKey, isPersistent: false);
    }

    public async Task<User> AutoProvisionUserAsync(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = name
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new Exception("Failed to create user");

        return user;
    }

    public async Task<IdentityResult> AddLoginAsync(User user, ExternalLoginInfo info)
    {
        return await _userManager.AddLoginAsync(user, info);
    }

    public async Task SignInAsync(User user)
    {
        await _signInManager.SignInAsync(user, isPersistent: false);
    }
}



