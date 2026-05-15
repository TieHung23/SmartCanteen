using SC.Contract.Services.Auth;

namespace SC.Infrastructure.Services.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainText)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainText, WorkFactor);
    }

    public bool Verify(string plainText, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainText, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
