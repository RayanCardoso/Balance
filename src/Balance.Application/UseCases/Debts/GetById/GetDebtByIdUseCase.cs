using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationCreditorType = Balance.Communication.Enums.CreditorType;
using CommunicationDebtMode = Balance.Communication.Enums.DebtMode;
using CommunicationExpenseStatus = Balance.Communication.Enums.ExpenseStatus;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace Balance.Application.UseCases.Debts.GetById;

public class GetDebtByIdUseCase : IGetDebtByIdUseCase
{
    private readonly IDebtReadOnlyRepository _debtReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetDebtByIdUseCase(IDebtReadOnlyRepository debtReadOnlyRepository, ILoggedUser loggedUser)
    {
        _debtReadOnlyRepository = debtReadOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseDebtJson> Execute(Guid id)
    {
        var loggedUser = await _loggedUser.Get();

        var debt = await _debtReadOnlyRepository.GetById(loggedUser, id)
            ?? throw new NotFoundException(ResourceErrorMessages.DEBT_NOT_FOUND);

        // Re-sorted here rather than trusted from the include: the in-memory provider and
        // PostgreSQL have disagreed on unordered Include ordering before, and a re-priced bill
        // displaying a superseded amount is exactly the defect that cost this project once.
        var installments = debt.Installments.OrderBy(installment => installment.Number).ToList();
        var payments = debt.Payments.OrderBy(payment => payment.PaymentDate).ToList();

        return new ResponseDebtJson
        {
            Id = debt.Id,
            Name = debt.Name,
            Mode = (CommunicationDebtMode)debt.Mode,
            CreditorId = debt.CreditorId,
            CreditorName = debt.Creditor.Name,
            CreditorType = (CommunicationCreditorType)debt.Creditor.Type,
            PersonId = debt.PersonId,
            CategoryId = debt.CategoryId,
            CategoryName = debt.Category.Name,
            PrincipalAmount = debt.PrincipalAmount,
            TotalAmount = debt.TotalAmount,
            StartDate = debt.StartDate,
            DueDay = debt.DueDay,
            InstallmentCount = debt.InstallmentCount,
            EndMonth = debt.EndMonth,
            Archived = debt.Archived,
            Notes = debt.Notes,
            OutstandingBalance = debt.OutstandingBalance(),
            IsSettled = debt.IsSettled(),
            Installments = installments
                .Select(installment => BuildInstallmentLine(installment, payments))
                .ToList(),
            Payments = payments.Select(BuildPaymentLine).ToList()
        };
    }

    private static ResponseDebtInstallmentJson BuildInstallmentLine(
        DebtInstallment installment,
        List<DebtPayment> payments)
    {
        // The unique index on DebtInstallmentId means at most one payment settles a given
        // installment - see RegisterDebtPaymentUseCase.
        var payment = payments.FirstOrDefault(p => p.DebtInstallmentId == installment.Id);

        return new ResponseDebtInstallmentJson
        {
            Id = installment.Id,
            Number = installment.Number,
            ReferenceMonth = installment.ReferenceMonth,
            DueDate = installment.DueDate,
            ExpectedAmount = installment.ExpectedAmount,
            AmountPaid = payment?.AmountPaid,
            PaymentId = payment?.Id,
            Status = ResolveStatus(installment.ExpectedAmount, payment?.AmountPaid)
        };
    }

    private static ResponseDebtPaymentJson BuildPaymentLine(DebtPayment payment) => new()
    {
        Id = payment.Id,
        DebtId = payment.DebtId,
        DebtInstallmentId = payment.DebtInstallmentId,
        ReferenceMonth = payment.ReferenceMonth,
        PaymentDate = payment.PaymentDate,
        AmountPaid = payment.AmountPaid,
        Type = (CommunicationExpenseType?)payment.Type,
        AccountId = payment.AccountId,
        AccountName = payment.Account?.Name,
        Notes = payment.Notes
    };

    /// <summary>
    /// Mirrors GetMonthlyExpenseUseCase's rule: nothing paid is Pending, and a paid installment
    /// diverges only when the amount does not match what was expected.
    /// </summary>
    private static CommunicationExpenseStatus ResolveStatus(decimal expectedAmount, decimal? actualAmount)
    {
        if (actualAmount is null)
        {
            return CommunicationExpenseStatus.Pending;
        }

        return actualAmount == expectedAmount
            ? CommunicationExpenseStatus.Paid
            : CommunicationExpenseStatus.Divergent;
    }
}
