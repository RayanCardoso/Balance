using Balance.Communication.Requests;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;

public interface IRegisterInstallmentPlanUseCase
{
    Task<ResponseInstallmentPlanJson> Execute(RequestRegisterInstallmentPlanJson request);
}
