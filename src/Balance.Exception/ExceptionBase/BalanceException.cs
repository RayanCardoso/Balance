namespace Balance.Exception.ExceptionBase;

public abstract class BalanceException : SystemException
{
    protected BalanceException(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract List<string> GetErrors();
}
