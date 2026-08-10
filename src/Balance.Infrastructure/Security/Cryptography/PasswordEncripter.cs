using Balance.Domain.Security.Cryptography;

namespace Balance.Infrastructure.Security.Cryptography;

internal class PasswordEncripter : IPasswordEncripter
{
    public string Encrypt(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
