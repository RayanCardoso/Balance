using Balance.Domain.Entities;

namespace Balance.Domain.Security.Tokens;

public interface IAccessTokenGenerator
{
    string Generate(User user);
}
