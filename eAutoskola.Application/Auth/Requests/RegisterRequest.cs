namespace eAutoskola.Application.Auth.Requests;

/// <summary>Student self-registration only - Admin/Instructor accounts are created by an Admin
/// via /api/users (prompt 2). Never accepts a role/isAdmin value from the client
/// (upute.md §5).</summary>
public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public int? CityId { get; set; }
    public int VehicleCategoryId { get; set; }
}
