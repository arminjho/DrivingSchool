namespace eAutoskola.Infrastructure.Security;

/// <summary>
/// BCrypt only - never unsalted SHA256/HMAC/custom schemes (upute.md Appendix A.3). Used by
/// both the seeder and AuthController/UserService so seed and runtime hashes are always the
/// same format.
/// </summary>
public static class PasswordHasher
{
    private const int WorkFactor = 12;

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
