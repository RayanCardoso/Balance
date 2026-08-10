using System.Net;

namespace Balance.Exception.ExceptionBase;

public class NotFoundException : BalanceException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.NotFound;

    public override List<string> GetErrors() => [Message];
}
