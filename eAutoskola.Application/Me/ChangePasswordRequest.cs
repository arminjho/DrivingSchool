namespace eAutoskola.Application.Me;

/// <summary>Self-service password change - old password is always required, unlike an Admin
/// editing another user (upute.md §4).</summary>
public class ChangePasswordRequest
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}
