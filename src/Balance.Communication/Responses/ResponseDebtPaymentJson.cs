using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseDebtPaymentJson
{
    public Guid Id { get; set; }
    public Guid DebtId { get; set; }

    /// <summary>Null on an OpenEnded debt's payment, which settles no particular installment.</summary>
    public Guid? DebtInstallmentId { get; set; }

    /// <summary>Copied from the installment when there is one; derived from the payment date when there is not.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public DateOnly PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>How it was paid. Null when the caller did not record it.</summary>
    public ExpenseType? Type { get; set; }

    /// <summary>Null when it did not come out of a registered account - a Pix or cash.</summary>
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }

    public string? Notes { get; set; }
}
