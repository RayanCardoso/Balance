using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Debts.GetMonthly;

/// <summary>
/// Assembles one month's debt obligations: one line per Scheduled installment falling in the month,
/// one line per OpenEnded payment recorded in it, and the three totals that tell how much of the
/// month is already spoken for. Line-building and status logic live entirely in
/// <see cref="DebtMonthLineBuilder"/>; this use case only resolves the month, reads through the
/// repository and sums.
/// </summary>
public class GetMonthlyDebtUseCase : IGetMonthlyDebtUseCase
{
    private readonly IDebtReadOnlyRepository _debtReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetMonthlyDebtUseCase(IDebtReadOnlyRepository debtReadOnlyRepository, ILoggedUser loggedUser)
    {
        _debtReadOnlyRepository = debtReadOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseMonthlyDebtJson> Execute(int year, int month)
    {
        var competenceMonth = BuildCompetenceMonth(year, month);

        // Resolved once, up front, and threaded through every BuildScheduled call below - never
        // read from the clock per line, so an overdue assertion stays under the caller's control
        // rather than the moment the request happens to land.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var loggedUser = await _loggedUser.Get();

        var debts = await _debtReadOnlyRepository.GetForMonth(loggedUser, competenceMonth);

        var lines = debts.SelectMany(debt => BuildLines(debt, today)).ToList();

        return new ResponseMonthlyDebtJson
        {
            CompetenceMonth = competenceMonth,
            Lines = lines,
            TotalExpected = lines.Sum(line => line.ExpectedAmount ?? 0m),
            TotalPaid = lines.Sum(line => line.AmountPaid ?? 0m),
            TotalCommitted = lines.Sum(Committed)
        };
    }

    private static IEnumerable<ResponseMonthlyDebtLineJson> BuildLines(Debt debt, DateOnly today)
    {
        if (debt.Mode == DebtMode.Scheduled)
        {
            return debt.Installments.Select(installment =>
            {
                // The unique index on DebtInstallmentId means at most one payment settles a given
                // installment - see RegisterDebtPaymentUseCase.
                var payment = debt.Payments.FirstOrDefault(p => p.DebtInstallmentId == installment.Id);

                return DebtMonthLineBuilder.BuildScheduled(debt, installment, payment, today);
            });
        }

        return debt.Payments.Select(payment => DebtMonthLineBuilder.BuildOpenEnded(debt, payment));
    }

    /// <summary>
    /// What the month actually costs for one line: the amount paid when it exists, and the expected
    /// amount when it does not. An OpenEnded line has no expected amount, so an unpaid one - which
    /// cannot occur, since it only exists because a payment was recorded - would cost nothing.
    /// </summary>
    private static decimal Committed(ResponseMonthlyDebtLineJson line) =>
        line.AmountPaid ?? line.ExpectedAmount ?? 0m;

    private static DateOnly BuildCompetenceMonth(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.REFERENCE_MONTH_INVALID]);
        }

        return new DateOnly(year, month, 1);
    }
}
