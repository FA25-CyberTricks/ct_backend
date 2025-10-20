using ct.backend.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ct.backend.Common.Ports.GoogleAuth
{
    public interface IGoogleAuthService
    {
        Task<ExternalLoginInfo?> GetExternalLoginInfoAsync();
        Task<SignInResult> ExternalLoginSignInAsync(string provider, string providerKey);
        Task<User> AutoProvisionUserAsync(ExternalLoginInfo info);
        Task<IdentityResult> AddLoginAsync(User user, ExternalLoginInfo info);
        Task SignInAsync(User user);
    }
}
