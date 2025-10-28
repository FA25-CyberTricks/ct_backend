using ct.backend.Common.Ports.Storage;
using ct.backend.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ct.backend.Features.UserProfiles
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // yêu cầu login
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IGoogleStorageService _googleStorageService;

        public ProfileController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IGoogleStorageService googleStorageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _googleStorageService = googleStorageService;
        }

        private Task<User?> GetCurrentUserAsync() => _userManager.GetUserAsync(User);

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user is null) return Unauthorized(new { message = "Not logged in" });

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                gender = (int?)user.Gender,                
                genderName = user.Gender.ToString()?.ToLower(),
                birth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                phoneNumber = user.PhoneNumber,
                firstName = user.FirstName,
                lastName = user.LastName,
                avatarUrl = user.AvatarUrl,
                address = user.Address,
            });
        }

        [HttpPut("update")]
        [Authorize]
        [RequestSizeLimit(20_000_000)] // giới hạn 10MB
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request, IFormFile? avatarFile)
        {
            var user = await GetCurrentUserAsync();
            if (user is null) return Unauthorized(new { message = "Not logged in" });

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName.Trim();

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber.Trim();

            if (!string.IsNullOrWhiteSpace(request.Address))
                user.Address = request.Address.Trim();

            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;

            if (request.Gender.HasValue)
                user.Gender = request.Gender.Value;

            if (avatarFile is { Length: > 0 })
            {
                // Kiểm tra MIME cơ bản (chỉ nhận ảnh)
                var allowed = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif" };
                if (!allowed.Contains(avatarFile.ContentType))
                    return BadRequest(new { message = "File ảnh không hợp lệ. Chỉ chấp nhận PNG/JPEG/WEBP/GIF." });

                // Lấy phần mở rộng từ tên file (fallback theo contentType nếu không có)
                var ext = Path.GetExtension(avatarFile.FileName);
                if (string.IsNullOrWhiteSpace(ext))
                    ext = avatarFile.ContentType switch
                    {
                        "image/png" => ".png",
                        "image/jpeg" => ".jpg",
                        "image/jpg" => ".jpg",
                        "image/webp" => ".webp",
                        "image/gif" => ".gif",
                        _ => ".bin"
                    };

                // Tạo object name theo cấu trúc bucket (ví dụ có prefix public/)
                // Bạn có thể đổi "public/avatars" theo convention dự án
                var objectName = $"public/avatars/{user.Id}/{Guid.NewGuid():N}{ext}";

                // Dùng stream trực tiếp từ IFormFile
                using Stream stream = avatarFile.OpenReadStream();

                // Có thể dùng token để hủy khi client hủy request
                var ct = HttpContext.RequestAborted;

                // Upload và nhận về URL/path đã lưu (service trả về string)
                string savedUrl = await _googleStorageService.UploadAsync(
                    stream,
                    objectName,
                    avatarFile.ContentType ?? "application/octet-stream",
                    ct
                );

                if(user.AvatarUrl != null)
                {
                    await _googleStorageService.DeleteAsync(user.AvatarUrl.Replace(_googleStorageService.GetPublicUrl(""), ""), ct);
                }
                // Lưu URL vào user (tuỳ bạn muốn lưu full URL hay objectName)
                user.AvatarUrl = _googleStorageService.GetPublicUrl(objectName);
            }

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = "Update failed", errors = result.Errors });

            return Ok(new { message = "Profile updated successfully", avatarUrl = user.AvatarUrl });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user is null) return Unauthorized(new { message = "Not logged in" });

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Change password failed", errors = result.Errors });

            // ❌ JWT là stateless, không có cookie sign-in để "refresh".
            // ✅ Nếu cần, hãy yêu cầu FE lấy token mới (refresh-token flow) sau khi đổi mật khẩu.
            return Ok(new { message = "Password changed successfully" });
        }
    }
}