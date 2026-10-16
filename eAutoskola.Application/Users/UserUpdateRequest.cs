namespace eAutoskola.Application.Users;

/// <summary>No password required to edit a user; NewPassword is only validated/applied if
/// provided (upute.md §4).</summary>
public class UserUpdateRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int? CityId { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}
