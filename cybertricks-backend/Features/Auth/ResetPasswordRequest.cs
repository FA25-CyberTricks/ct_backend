namespace ct.backend.Features.Auth;

public class ResetPasswordRequest
{
    public string UserId { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}