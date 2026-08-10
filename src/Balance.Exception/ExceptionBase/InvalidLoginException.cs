using System.Net;

namespace Balance.Exception.ExceptionBase;

public class InvalidLoginException : BalanceException
{
    public InvalidLoginException() : base(ResourceErrorMessages.EMAIL_OR_PASSWORD_INVALID)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.Unauthorized;

    public override List<string> GetErrors() => [Message];
}
