using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Debts.GetAll;

public interface IGetAllDebtsUseCase
{
    /// <summary>
    /// The logged user's debts, optionally narrowed to one creditor or one person. Archived and
    /// settled debts are excluded unless <paramref name="includeInactive"/> is true - settled is
    /// derived from payments and filtered here rather than in the repository.
    /// </summary>
    Task<ResponseDebtsJson> Execute(Guid? creditorId, Guid? personId, bool includeInactive);
}
