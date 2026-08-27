using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Debts;

public interface IDebtPaymentRepository
{
    Task Add(DebtPayment payment);

    /// <summary>
    /// Tracked read, so a correction can overwrite the payment in place. Null when the payment is
    /// not reachable from <paramref name="user"/>.
    /// </summary>
    Task<DebtPayment?> GetById(User user, Guid id);

    /// <summary>
    /// The payment already recorded against that installment, or null. Backs the
    /// one-payment-per-installment probe: the unique index is defence in depth, and the in-memory
    /// provider the integration tests run on does not enforce it.
    /// </summary>
    Task<DebtPayment?> GetByInstallment(User user, Guid installmentId);
}
