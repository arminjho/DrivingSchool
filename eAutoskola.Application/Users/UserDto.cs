namespace eAutoskola.Application.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int? CityId { get; set; }
    public string? CityName { get; set; }
    public required string Role { get; set; }
    public bool IsActive { get; set; }
    public string? ProfileImagePath { get; set; }
    public int? VehicleCategoryId { get; set; }
}
