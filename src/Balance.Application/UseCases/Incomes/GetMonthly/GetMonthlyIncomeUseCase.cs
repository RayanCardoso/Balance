using Balance.Communication.Responses;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories.Incomes;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationIncomeStatus = Balance.Communication.Enums.IncomeStatus;
using CommunicationIncomeType = Balance.Communication.Enums.IncomeType;
using DomainIncomeType = Balance.Domain.Enums.IncomeType;

namespace Balance.Application.UseCases.Incomes.GetMonthly;

public class GetMonthlyIncomeUseCase : IGetMonthlyIncomeUseCase
{
    private readonly IIncomeSourceReadOnlyRepository _readOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetMonthlyIncomeUseCase(
        IIncomeSourceReadOnlyRepository readOnlyRepository,
        ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseMonthlyIncomeJson> Execute(int year, int month)
    {
        var referenceMonth = BuildReferenceMonth(year, month);

        var loggedUser = await _loggedUser.Get();

        var incomeSources = await _readOnlyRepository.GetForMonth(loggedUser, referenceMonth);

        var lines = new List<ResponseMonthlyIncomeLineJson>();

        foreach (var incomeSource in incomeSources)
        {
            var version = incomeSource.Type == DomainIncomeType.Recurring
                ? incomeSource.VersionInEffect(referenceMonth)
                : null;

            var receivedAmount = incomeSource.Payments.Sum(payment => payment.AmountReceived);
            var expectedAmount = version?.Amount;

            lines.Add(new ResponseMonthlyIncomeLineJson
            {
                IncomeSourceId = incomeSource.Id,
                Name = incomeSource.Name,
                Type = (CommunicationIncomeType)incomeSource.Type,
                PersonId = incomeSource.PersonId,
                ExpectedAmount = expectedAmount,
                ExpectedDay = version?.ExpectedDay,
                ReceivedAmount = receivedAmount,
                Status = ResolveStatus(expectedAmount, receivedAmount)
            });
        }

        return new ResponseMonthlyIncomeJson
        {
            ReferenceMonth = referenceMonth,
            Lines = lines,
            TotalExpected = lines.Sum(line => line.ExpectedAmount ?? 0m),
            TotalReceived = lines.Sum(line => line.ReceivedAmount)
        };
    }

    /// <summary>
    /// Evaluated in this order so the rules cannot overlap: nothing received is Pending;
    /// a Variable source with money in is Received, since it has no expectation to diverge from.
    /// </summary>
    private static CommunicationIncomeStatus ResolveStatus(decimal? expectedAmount, decimal receivedAmount)
    {
        if (receivedAmount == 0m)
        {
            return CommunicationIncomeStatus.Pending;
        }

        if (expectedAmount is null)
        {
            return CommunicationIncomeStatus.Received;
        }

        return receivedAmount == expectedAmount
            ? CommunicationIncomeStatus.Received
            : CommunicationIncomeStatus.Divergent;
    }

    private static DateOnly BuildReferenceMonth(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.REFERENCE_MONTH_INVALID]);
        }

        return new DateOnly(year, month, 1);
    }
}
