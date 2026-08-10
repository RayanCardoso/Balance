using Balance.Domain.Security.Tokens;
using Moq;

namespace CommonTestUtilities.Token;

public class TokenProviderBuilder
{
    public static ITokenProvider Build(string token)
    {
        var mock = new Mock<ITokenProvider>();

        mock.Setup(provider => provider.TokenOnRequest()).Returns(token);

        return mock.Object;
    }
}
