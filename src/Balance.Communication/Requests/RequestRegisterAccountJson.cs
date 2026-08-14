namespace Balance.Communication.Requests;

public class RequestRegisterAccountJson
{
    public string Name { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    /// <summary>Null on an account that is not a credit card.</summary>
    public int? ClosingDay { get; set; }

    /// <summary>Null on an account that is not a credit card.</summary>
    public int? DueDay { get; set; }

    /// <summary>Null on an account that is not a credit card. Stored for display only.</summary>
    public decimal? Limit { get; set; }
}
