using ct.backend.Common.Constants;
using ct.backend.Common.Ports.GoogleAuth;
using ct.backend.Common.Ports.Mail;
using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ct.backend.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BookingDbContext _db; // <- DbContext của bạn
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IMailService _mailService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IGoogleAuthService _googleAuthService; // <- Google auth service

        public AuthController(
            BookingDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AuthController> logger,
            IMailService mailService,
            IConfiguration config,
            IWebHostEnvironment env,
            IGoogleAuthService googleAuthService)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _mailService = mailService;
            _config = config;
            _env = env;
            _googleAuthService = googleAuthService;
        }

        /// <summary>
        /// LOGIN (trả JWT)
        /// </summary>
        /// <summary>LOGIN (Access + Refresh)</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Email)
                       ?? await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
                return Unauthorized(new { message = "User does not exist" });

            var pwResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!pwResult.Succeeded)
                return Unauthorized(new { message = "Wrong password" });

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized(new { message = "Email not confirmed" });

            // Access token
            var jwt = await GenerateJwtAsync(user);
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            // Refresh token
            var refreshPlain = GenerateRefreshTokenPlaintext();
            var refreshHash = HashToken(refreshPlain);

            var rt = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:ExpireDays"] ?? "14")),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();

            // Set HttpOnly cookie
            Response.Cookies.Append("refreshToken", refreshPlain, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = rt.ExpiresAtUtc
            });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token = jwt,
                expires = parsed.ValidTo,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    avatarUrl = user.AvatarUrl,
                    role = roles
                },
                returnUrk = request.returnUrl ?? "/"
            });
        }

        /// <summary>
        /// REFRESH (rotate)
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(string? returnUrl)
        {
            var refreshPlain = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshPlain))
                return Unauthorized(new { message = "Missing refresh token" });

            var refreshHash = HashToken(refreshPlain);
            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshHash);

            if (rt == null || rt.RevokedAtUtc != null || rt.ExpiresAtUtc <= DateTime.UtcNow || rt.IsUsed)
                return Unauthorized(new { message = "Invalid/expired refresh token" });

            var user = await _userManager.FindByIdAsync(rt.UserId);
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            // mark used & rotate
            rt.IsUsed = true;
            rt.RevokedAtUtc = DateTime.UtcNow;
            rt.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var newPlain = GenerateRefreshTokenPlaintext();
            var newHash = HashToken(newPlain);
            var newRt = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:ExpireDays"] ?? "14")),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            rt.ReplacedByTokenId = newRt.Id;

            _db.RefreshTokens.Add(newRt);
            await _db.SaveChangesAsync();

            // cấp access token mới
            var newJwt = await GenerateJwtAsync(user);
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(newJwt);

            // Set lại cookie
            Response.Cookies.Append("refreshToken", newPlain, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = newRt.ExpiresAtUtc
            });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token = newJwt,
                expires = parsed.ValidTo,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    avatarUrl = user.AvatarUrl,
                    role = roles
                },
                returnUrl = returnUrl ?? "/"
            });
        }

        /// <summary>
        /// LOGOUT (revoke refresh hiện tại)
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshPlain = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshPlain))
            {
                var hash = HashToken(refreshPlain);
                var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
                if (rt != null && rt.RevokedAtUtc == null)
                {
                    rt.RevokedAtUtc = DateTime.UtcNow;
                    rt.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _db.SaveChangesAsync();
                }
            }

            // xoá cookie client
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logged out" });
        }

        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = "/")
        {
            // FE gọi endpoint này → BE redirect qua Google
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(props, "Google");
        }

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
        {
            var info = await _googleAuthService.GetExternalLoginInfoAsync();
            if (info == null)
                return Content("<script>window.opener.postMessage({ error: 'GoogleLoginFailed' }, '*');window.close();</script>", "text/html");

            // Check login or auto create
            var signInResult = await _googleAuthService.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey);
            User? user;

            if (signInResult.Succeeded)
            {
                user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);
                var avatar = info.Principal.FindFirstValue("urn:google:picture");

                if (string.IsNullOrEmpty(email))
                    return Content("<script>window.opener.postMessage({ error: 'NoEmail' }, '*');window.close();</script>", "text/html");

                // Split name
                var parts = (name ?? email).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = parts.FirstOrDefault() ?? email;
                var lastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";

                user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    await _googleAuthService.AddLoginAsync(user, info);
                }
                else
                {
                    user = new User
                    {
                        UserName = email,
                        Email = email,
                        FullName = name ?? email,
                        FirstName = firstName,
                        LastName = lastName,
                        AvatarUrl = avatar ?? _config["AppSettings:DefaultAvatarUrl"],
                        EmailConfirmed = true
                    };

                    var create = await _userManager.CreateAsync(user);
                    if (!create.Succeeded)
                    {
                        var err = create.Errors.FirstOrDefault()?.Description;
                        return Content($"<script>window.opener.postMessage({{ error: 'Failed: {err}' }}, '*');window.close();</script>", "text/html");
                    }

                    await _userManager.AddToRoleAsync(user, "User");
                    await _googleAuthService.AddLoginAsync(user, info);
                }
            }

            if (user == null)
                return Content("<script>window.opener.postMessage({ error: 'UserNotFound' }, '*');window.close();</script>", "text/html");

            // JWT + refresh
            var jwt = await GenerateJwtAsync(user);
            var refreshPlain = GenerateRefreshTokenPlaintext();
            var refreshHash = HashToken(refreshPlain);
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:ExpireDays"] ?? "14")),
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _db.SaveChangesAsync();

            Response.Cookies.Append("refreshToken", refreshPlain, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:ExpireDays"] ?? "14"))
            });

            var roles = await _userManager.GetRolesAsync(user);

            // ✅ Gửi token + user về FE qua postMessage và tự đóng popup
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                token = jwt,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    avatarUrl = user.AvatarUrl,
                    role = roles
                },
                returnUrl
            });

            // LƯU Ý: Lấy origin của FE từ query ?opener=... (nếu có) hoặc fallback sang document.referrer
            var html = $@"<!doctype html>
                        <html>
                          <body>
                            <script>
                              (function() {{
                                try {{
                                  var params = new URLSearchParams(window.location.search);
                                  var openerFromQuery = params.get('opener');
                                  var openerOrigin = openerFromQuery || (document.referrer ? new URL(document.referrer).origin : '*');

                                  // payload đã được server serialize ở biến C# {nameof(json)}:
                                  var payload = {json};

                                  if (window.opener && !window.opener.closed) {{
                                    window.opener.postMessage(payload, openerOrigin);
                                  }}
                                }} catch (e) {{
                                  // im lặng
                                }} finally {{
                                  // defer 1 tick để đảm bảo postMessage flush trước khi đóng
                                  setTimeout(function() {{ window.close(); }}, 0);
                                }}
                              }})();
                            </script>
                          </body>
                        </html>";
            return Content(html, "text/html; charset=utf-8");

        }

        /// <summary>
        /// REGISTER
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1) Check tồn tại
            var existed = await _userManager.FindByEmailAsync(request.Email);
            if (existed != null)
                return BadRequest(new { message = "UserName or Email is existed" });
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "The password and the confirmation password are mismatched" });

            // 2) Tạo user
            Random random = new Random();
            var user = new User
            {
                UserName = request.Email.Split('@')[0]
                    + $"{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(0, 10)}",
                AvatarUrl = GoogleStoragePaths.DefaultAvatars,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {request.LastName}",
                Email = request.Email,
                EmailConfirmed = false
            };

            var create = await _userManager.CreateAsync(user, request.Password);
            if (!create.Succeeded)
            {
                var errorMessage = create.Errors.FirstOrDefault()?.Description;

                return BadRequest(new { message = errorMessage });
            }

            // (optional) gán role mặc định
            await _userManager.AddToRoleAsync(user, "User");

            // 3) Tạo token confirm + encode an toàn cho URL
            var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            // 4) Build absolute confirm URL (giống MVC: Url.Action + Request.Scheme)
            var confirmUrl = Url.Action(
                action: nameof(ConfirmEmail),
                controller: "Auth",
                values: new { userId = user.Id, token = encodedToken, returnUrl = request.ReturnUrl },
                protocol: Request.Scheme
            );

            // 5) Load template & thay placeholder
            var emailBody = await BuildEmailBodyAsync(
                templateFileName: "ConfirmEmailTemplate.html",
                confirmationUrl: confirmUrl!,
                email: request.Email
            );

            // 6) Gửi mail
            await _mailService.SendMailAsync(new MailContent
            {
                To = request.Email,
                Subject = "Confirm Email CyberTricks",
                Body = emailBody
            });

            _logger.LogInformation("User registered, confirmation email sent.");

            return Ok(new
            {
                message = "Registration successful. Please check your email to confirm your account.",
                confirmUrl // giữ để dev debug; prod có thể bỏ
            });
        }

        /// <summary>
        /// CONFIRM EMAIL
        /// </summary>
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token, [FromQuery] string? returnUrl = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return BadRequest(new { message = "User does not exist" });

            // Decode token về dạng raw
            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var rawToken = Encoding.UTF8.GetString(decodedBytes);

            var result = await _userManager.ConfirmEmailAsync(user, rawToken);
            if (!result.Succeeded)
                return BadRequest(new { message = "Email confirmation failed", errors = result.Errors });

            // Option 1: Redirect về FE nếu có returnUrl (SPA/FE site)
            if (!string.IsNullOrWhiteSpace(returnUrl))
                return Redirect(returnUrl);

            // Option 2: Trả JSON
            return Ok(new { message = "Email confirmation successful" });
        }

        /// <summary>
        /// Helpers
        /// </summary>
        private async Task<string> BuildEmailBodyAsync(string templateFileName, string confirmationUrl, string email)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Common", "Templates", templateFileName);

            var html = await System.IO.File.ReadAllTextAsync(templatePath);
            return html
                .Replace("{{ .ConfirmationURL }}", confirmationUrl)
                .Replace("{{ .Email }}", email);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Không tiết lộ thông tin user tồn tại hay không
                return Ok(new { message = "If your email is registered, you will receive a password reset email." });
            }

            // 1. Tạo token reset password
            var rawToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            // 2. Tạo link reset password
            var resetUrl = Url.Action(
                action: nameof(ResetPassword),
                controller: "Auth",
                values: new { userId = user.Id, token = encodedToken, returnUrl = request.ReturnUrl },
                protocol: Request.Scheme
            );

            // 3. Load template & thay placeholder
            var emailBody = await BuildEmailBodyAsync(
                templateFileName: "ResetPasswordTemplate.html",
                confirmationUrl: resetUrl!,
                email: user.Email
            );

            // 4. Gửi mail
            await _mailService.SendMailAsync(new MailContent
            {
                To = user.Email,
                Subject = "Reset your CyberTricks password",
                Body = emailBody
            });

            return Ok(new { message = "If your email is registered, you will receive a password reset email." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return BadRequest(new { message = "Invalid user." });

            var decodedBytes = WebEncoders.Base64UrlDecode(request.Token);
            var rawToken = Encoding.UTF8.GetString(decodedBytes);

            var result = await _userManager.ResetPasswordAsync(user, rawToken, request.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Reset password failed", errors = result.Errors });

            return Ok(new { message = "Password has been reset successfully." });
        }

        private async Task<string> GenerateJwtAsync(User user)
        {
            var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
            var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
            var secret = _config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
            if (secret.Length < 32) throw new InvalidOperationException("Jwt:Key should be at least 32 characters.");

            var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 60;
            var now = DateTime.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);

            // chỉ giữ claim ngắn, đủ dùng
            var claims = new List<Claim>
            {
                new("sub",  user.Id),
                new("name", user.UserName ?? string.Empty),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            };
            claims.AddRange(roles.Select(r => new Claim("role", r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshTokenPlaintext()
        {
            var bytes = RandomNumberGenerator.GetBytes(32); // 256-bit
            return Base64UrlEncode(bytes);
        }

        private static string HashToken(string tokenPlaintext)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(tokenPlaintext));
            return Convert.ToHexString(hash); // hoặc Base64Url cho ngắn
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
