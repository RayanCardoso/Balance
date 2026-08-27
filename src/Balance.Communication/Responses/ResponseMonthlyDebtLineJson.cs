using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

/// <summary>
/// One line of a debt's monthly view. A <c>Scheduled</c> debt contributes one line per installment
/// falling in the month, carrying an expected amount and possibly a payment. An <c>OpenEnded</c>
/// debt has no schedule, so it contributes one line per payment recorded in the month instead -
/// <see cref="InstallmentNumber"/>, <see cref="InstallmentCount"/>, <see cref="DueDate"/> and
/// <see cref="ExpectedAmount"/> are left null on that line.
/// </summary>
public class ResponseMonthlyDebtLineJson
{
    public Guid DebtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DebtMode Mode { get; set; }

    /// <summary>Who is owed.</summary>
    public Guid CreditorId { get; set; }
    public string CreditorName { get; set; } = string.Empty;
    public CreditorType CreditorType { get; set; }

    /// <summary>Who in the household owes it.</summary>
    public Guid PersonId { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Null on an OpenEnded line, which has no installment to number.</summary>
    public int? InstallmentNumber { get; set; }

    /// <summary>Null on an OpenEnded line, which has no schedule to count against.</summary>
    public int? InstallmentCount { get; set; }

    /// <summary>Null on an OpenEnded line, which has no schedule to be due against.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Null on an OpenEnded line, which has no installment to expect an amount from.</summary>
    public decimal? ExpectedAmount { get; set; }

    /// <summary>Null on a Scheduled line whose installment has not been paid yet.</summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>Null on a Scheduled line whose installment has not been paid yet.</summary>
    public DateOnly? PaymentDate { get; set; }

    /// <summary>
    /// The id of the payment behind this line, null on a Scheduled line whose installment has not
    /// been paid yet. Without it a client reading a monthly line cannot reach the payment endpoints.
    /// </summary>
    public Guid? PaymentId { get; set; }

    public string? Notes { get; set; }

    public ExpenseStatus Status { get; set; }

    /// <summary>
    /// True only for a Scheduled line that is still Pending with a due date strictly before the
    /// resolved <c>today</c>. Always false on an OpenEnded line, which has no due date to miss.
    /// </summary>
    public bool IsOverdue { get; set; }
}
