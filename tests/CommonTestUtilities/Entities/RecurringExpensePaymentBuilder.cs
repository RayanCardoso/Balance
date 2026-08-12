using Balance.Domain.Entities;

namespace CommonTestUtilities.Entities;

public class RecurringExpensePaymentBuilder
{
    /// <summary>A payment already recorded against the expense's version in effect.</summary>
    public static RecurringExpensePayment Build(
        RecurringExpense recurringExpense,
        DateOnly? referenceMonth = null,
        decimal amountPaid = 180.00m,
        DateOnly? paymentDate = null,
        string? notes = "bill arrived",
        Guid? accountId = null)
    {
        var month = referenceMonth ?? new DateOnly(2026, 8, 1);

        return new RecurringExpensePayment
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = recurringExpense.Id,
            RecurringExpenseVersionId = recurringExpense.Versions[0].Id,
            ReferenceMonth = month,
            PaymentDate = paymentDate ?? month.AddDays(9),
            AmountPaid = amountPaid,
            Notes = notes,
            AccountId = accountId
        };
    }
}
