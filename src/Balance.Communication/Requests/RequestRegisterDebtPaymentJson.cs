using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterDebtPaymentJson
{
    public Guid DebtId { get; set; }

    /// <summary>Null on an OpenEnded debt's payment, which settles no particular installment.</summary>
    public Guid? DebtInstallmentId { get; set; }

    public DateOnly PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>How it was paid. Null when the caller does not record it.</summary>
    public ExpenseType? Type { get; set; }

    /// <summary>Null when it did not come out of a registered account - a Pix or cash.</summary>
    public Guid? AccountId { get; set; }

    public string? Notes { get; set; }
}
