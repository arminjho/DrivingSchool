namespace eAutoskola.Application.Users;

public class UserInsertRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int? CityId { get; set; }
    public required string Role { get; set; }

    /// <summary>Required when Role is Student or Instructor.</summary>
    public int? VehicleCategoryId { get; set; }
}
