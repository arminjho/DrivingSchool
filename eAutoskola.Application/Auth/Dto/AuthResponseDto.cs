namespace eAutoskola.Application.Auth.Dto;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Role { get; set; }
}
