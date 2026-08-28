using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Creditors.GetAll;

public interface IGetAllCreditorsUseCase
{
    /// <summary>The logged user's creditors, excluding archived ones unless <paramref name="includeArchived"/> is true.</summary>
    Task<ResponseCreditorsJson> Execute(bool includeArchived);
}
