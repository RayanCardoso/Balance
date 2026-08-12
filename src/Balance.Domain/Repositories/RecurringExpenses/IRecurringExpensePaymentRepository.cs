using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.RecurringExpenses;

public interface IRecurringExpensePaymentRepository
{
    Task Add(RecurringExpensePayment payment);

    /// <summary>
    /// Tracked read, so a correction can overwrite the payment in place. Null when the payment is
    /// not reachable from <paramref name="user"/>.
    /// </summary>
    Task<RecurringExpensePayment?> GetById(User user, Guid paymentId);

    /// <summary>
    /// The payment already recorded for that recurring expense and reference month, or null.
    /// Backs the one-payment-per-month probe: the unique index is defence in depth, and the
    /// in-memory provider the integration tests run on does not enforce it.
    /// </summary>
    Task<RecurringExpensePayment?> GetByMonth(Guid recurringExpenseId, DateOnly referenceMonth);
}
