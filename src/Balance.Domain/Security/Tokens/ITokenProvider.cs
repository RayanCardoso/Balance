namespace Balance.Domain.Security.Tokens;

public interface ITokenProvider
{
    string TokenOnRequest();
}
