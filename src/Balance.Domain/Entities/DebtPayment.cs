using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

// The fact half. Type and account are decided here, not on the debt.
public class DebtPayment : BaseEntity
{
    public Guid DebtId { get; set; }
    public Debt Debt { get; set; } = null!;

    /// <summary>Null on an OpenEnded debt's payment, which settles no particular installment.</summary>
    public Guid? DebtInstallmentId { get; set; }
    public DebtInstallment? DebtInstallment { get; set; }

    /// <summary>Copied from the installment when there is one; derived from the payment date when there is not.</summary>
    public DateOnly ReferenceMonth { get; set; }

    public DateOnly PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>How it was paid. Null when the user did not record it.</summary>
    public ExpenseType? Type { get; set; }

    /// <summary>Null when it did not come out of a registered account - a Pix or cash.</summary>
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }

    public string? Notes { get; set; }
}
