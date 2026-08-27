using Balance.Communication.Responses;
using Balance.Domain.Entities;
using CommunicationCreditorType = Balance.Communication.Enums.CreditorType;
using CommunicationDebtMode = Balance.Communication.Enums.DebtMode;
using CommunicationExpenseStatus = Balance.Communication.Enums.ExpenseStatus;

namespace Balance.Application.UseCases.Debts.GetMonthly;

/// <summary>
/// Turns one debt row into one monthly line. A <c>Scheduled</c> debt contributes one line per
/// installment falling in the month; an <c>OpenEnded</c> debt, which has no schedule, contributes
/// one line per payment instead. Kept as a public static class - rather than private methods on the
/// use case that will call it - so each branch is unit-testable on its own.
/// </summary>
public static class DebtMonthLineBuilder
{
    /// <summary>
    /// Maps one installment plus the payment that settles it, if any. <paramref name="today"/> is
    /// resolved once by the caller and never read from the clock in here, so an overdue assertion
    /// stays under the test's control rather than the day it happens to run.
    /// </summary>
    public static ResponseMonthlyDebtLineJson BuildScheduled(
        Debt debt,
        DebtInstallment installment,
        DebtPayment? payment,
        DateOnly today)
    {
        var status = ResolveStatus(installment.ExpectedAmount, payment?.AmountPaid);

        return new ResponseMonthlyDebtLineJson
        {
            DebtId = debt.Id,
            Name = debt.Name,
            Mode = (CommunicationDebtMode)debt.Mode,
            CreditorId = debt.CreditorId,
            CreditorName = debt.Creditor.Name,
            CreditorType = (CommunicationCreditorType)debt.Creditor.Type,
            PersonId = debt.PersonId,
            CategoryId = debt.CategoryId,
            CategoryName = debt.Category.Name,
            InstallmentNumber = installment.Number,
            InstallmentCount = debt.InstallmentCount,
            DueDate = installment.DueDate,
            ExpectedAmount = installment.ExpectedAmount,
            AmountPaid = payment?.AmountPaid,
            PaymentDate = payment?.PaymentDate,
            PaymentId = payment?.Id,
            Notes = payment?.Notes,
            Status = status,
            // Pending and strictly before today - a due date landing exactly on today has not been
            // missed yet.
            IsOverdue = status == CommunicationExpenseStatus.Pending && installment.DueDate < today
        };
    }

    /// <summary>
    /// Maps one payment on an OpenEnded debt, which settles no particular installment. There is no
    /// expectation to diverge from and no due date to miss, so the line is always Paid and never
    /// overdue.
    /// </summary>
    public static ResponseMonthlyDebtLineJson BuildOpenEnded(Debt debt, DebtPayment payment) =>
        new()
        {
            DebtId = debt.Id,
            Name = debt.Name,
            Mode = (CommunicationDebtMode)debt.Mode,
            CreditorId = debt.CreditorId,
            CreditorName = debt.Creditor.Name,
            CreditorType = (CommunicationCreditorType)debt.Creditor.Type,
            PersonId = debt.PersonId,
            CategoryId = debt.CategoryId,
            CategoryName = debt.Category.Name,
            InstallmentNumber = null,
            InstallmentCount = null,
            DueDate = null,
            ExpectedAmount = null,
            AmountPaid = payment.AmountPaid,
            PaymentDate = payment.PaymentDate,
            PaymentId = payment.Id,
            Notes = payment.Notes,
            Status = ResolveStatus(expectedAmount: null, payment.AmountPaid),
            IsOverdue = false
        };

    /// <summary>
    /// Mirrors GetMonthlyExpenseUseCase.ResolveStatus exactly, branch for branch: nothing paid is
    /// Pending; a paid line with no expectation to diverge from is Paid; otherwise a match is Paid
    /// and a mismatch is Divergent. Reimplemented here on purpose rather than shared, so the two
    /// features stay independently editable.
    /// </summary>
    public static CommunicationExpenseStatus ResolveStatus(decimal? expectedAmount, decimal? actualAmount)
    {
        if (actualAmount is null)
        {
            return CommunicationExpenseStatus.Pending;
        }

        if (expectedAmount is null)
        {
            return CommunicationExpenseStatus.Paid;
        }

        return actualAmount == expectedAmount
            ? CommunicationExpenseStatus.Paid
            : CommunicationExpenseStatus.Divergent;
    }
}
