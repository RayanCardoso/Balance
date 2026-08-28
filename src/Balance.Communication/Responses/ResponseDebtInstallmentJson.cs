using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseDebtInstallmentJson
{
    public Guid Id { get; set; }
    public int Number { get; set; }

    /// <summary>Normalised to the first day of the month it falls in.</summary>
    public DateOnly ReferenceMonth { get; set; }

    /// <summary>The due day inside that month, clamped to the month's length.</summary>
    public DateOnly DueDate { get; set; }

    public decimal ExpectedAmount { get; set; }

    /// <summary>Null until a payment has been recorded against this installment.</summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>Null until a payment has been recorded against this installment.</summary>
    public Guid? PaymentId { get; set; }

    public ExpenseStatus Status { get; set; }
}
