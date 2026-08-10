using Balance.Domain.Security.Tokens;
using Balance.Infrastructure.Security.Tokens;

namespace CommonTestUtilities.Token;

public class JwtTokenGeneratorBuilder
{
    public static IAccessTokenGenerator Build() =>
        new JwtTokenGenerator(expirationTimeMinutes: 5, signingKey: "test-signing-key-with-enough-length-32b+");
}
