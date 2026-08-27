using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Creditors.GetSummary;

public interface IGetCreditorSummaryUseCase
{
    /// <summary>
    /// How much the logged user still owes one creditor. Archived and settled debts are excluded
    /// from every figure - a summary that counted a paid-off or shelved debt would answer the
    /// wrong question.
    /// </summary>
    Task<ResponseCreditorSummaryJson> Execute(Guid creditorId);
}
