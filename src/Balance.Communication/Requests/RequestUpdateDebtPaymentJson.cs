using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

/// <summary>
/// Carries no <c>DebtId</c> and no installment id - a correction may never move a payment to a
/// different debt or installment.
/// </summary>
public class RequestUpdateDebtPaymentJson
{
    public DateOnly PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>How it was paid. Null when the caller does not record it.</summary>
    public ExpenseType? Type { get; set; }

    /// <summary>Null when it did not come out of a registered account - a Pix or cash.</summary>
    public Guid? AccountId { get; set; }

    public string? Notes { get; set; }
}
