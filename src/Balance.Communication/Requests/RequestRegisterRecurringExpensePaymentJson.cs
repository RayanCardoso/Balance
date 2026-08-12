namespace Balance.Communication.Requests;

public class RequestRegisterRecurringExpensePaymentJson
{
    public Guid RecurringExpenseId { get; set; }

    /// <summary>Any day inside the month it refers to; normalised to the first day.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public DateOnly PaymentDate { get; set; }

    /// <summary>What the bill actually cost that month, overriding the estimate for that month only.</summary>
    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    /// <summary>The account that actually paid this month. Optional.</summary>
    public Guid? AccountId { get; set; }
}
