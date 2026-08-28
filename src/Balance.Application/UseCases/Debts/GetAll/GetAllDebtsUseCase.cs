using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using CommunicationCreditorType = Balance.Communication.Enums.CreditorType;
using CommunicationDebtMode = Balance.Communication.Enums.DebtMode;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace Balance.Application.UseCases.Debts.GetAll;

public class GetAllDebtsUseCase : IGetAllDebtsUseCase
{
    private readonly IDebtReadOnlyRepository _debtReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetAllDebtsUseCase(IDebtReadOnlyRepository debtReadOnlyRepository, ILoggedUser loggedUser)
    {
        _debtReadOnlyRepository = debtReadOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseDebtsJson> Execute(Guid? creditorId, Guid? personId, bool includeInactive)
    {
        var loggedUser = await _loggedUser.Get();

        var debts = await _debtReadOnlyRepository.GetAll(loggedUser, creditorId, personId, includeInactive);

        // The repository excludes archived debts when includeInactive is false, but settled is
        // derived from Payments and cannot be expressed in SQL - it is filtered here instead.
        if (includeInactive == false)
        {
            debts = debts.Where(debt => debt.IsSettled() == false).ToList();
        }

        return new ResponseDebtsJson
        {
            Debts = debts.Select(BuildDebtLine).ToList()
        };
    }

    private static ResponseDebtJson BuildDebtLine(Debt debt)
    {
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
            Payments = payments.Select(BuildPaymentLine).ToList()
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
}
