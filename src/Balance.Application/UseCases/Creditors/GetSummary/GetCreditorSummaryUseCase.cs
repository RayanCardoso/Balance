using Balance.Communication.Responses;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationCreditorType = Balance.Communication.Enums.CreditorType;

namespace Balance.Application.UseCases.Creditors.GetSummary;

public class GetCreditorSummaryUseCase : IGetCreditorSummaryUseCase
{
    private readonly ICreditorReadOnlyRepository _creditorReadOnlyRepository;
    private readonly IDebtReadOnlyRepository _debtReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetCreditorSummaryUseCase(
        ICreditorReadOnlyRepository creditorReadOnlyRepository,
        IDebtReadOnlyRepository debtReadOnlyRepository,
        ILoggedUser loggedUser)
    {
        _creditorReadOnlyRepository = creditorReadOnlyRepository;
        _debtReadOnlyRepository = debtReadOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseCreditorSummaryJson> Execute(Guid creditorId)
    {
        var loggedUser = await _loggedUser.Get();

        var creditor = await _creditorReadOnlyRepository.GetById(loggedUser, creditorId)
            ?? throw new NotFoundException(ResourceErrorMessages.CREDITOR_NOT_FOUND);

        var debts = await _debtReadOnlyRepository.GetByCreditor(loggedUser, creditorId);

        // Archived and settled debts answer a question nobody asked - "how much is still owed"
        // excludes anything already shelved or paid off, from every one of the four figures.
        var openDebts = debts
            .Where(debt => debt.Archived == false && debt.IsSettled() == false)
            .ToList();

        return new ResponseCreditorSummaryJson
        {
            Creditor = new ResponseCreditorJson
            {
                Id = creditor.Id,
                Name = creditor.Name,
                Type = (CommunicationCreditorType)creditor.Type,
                Contact = creditor.Contact,
                Notes = creditor.Notes,
                Archived = creditor.Archived
            },
            UnsettledDebtCount = openDebts.Count,
            TotalOwed = openDebts.Sum(debt => debt.TotalAmount),
            TotalPaid = openDebts.Sum(debt => debt.Payments.Sum(payment => payment.AmountPaid)),
            OutstandingBalance = openDebts.Sum(debt => debt.OutstandingBalance())
        };
    }
}
