using Balance.Domain.Entities;

namespace Balance.Domain.Extensions;

public static class DebtExtensions
{
    /// <summary>
    /// What is still owed. Never stored, because every payment recorded against the debt would
    /// otherwise require rewriting a cached total instead of just being trusted as the source
    /// of truth.
    /// </summary>
    public static decimal OutstandingBalance(this Debt debt) =>
        debt.TotalAmount - debt.Payments.Sum(payment => payment.AmountPaid);

    /// <summary>
    /// A debt counts as settled the moment nothing more is owed - including an overpayment,
    /// which leaves a negative balance rather than an error.
    /// </summary>
    public static bool IsSettled(this Debt debt) => debt.OutstandingBalance() <= 0;
}
