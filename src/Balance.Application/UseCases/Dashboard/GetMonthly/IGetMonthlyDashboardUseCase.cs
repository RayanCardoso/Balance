using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Dashboard.GetMonthly;

public interface IGetMonthlyDashboardUseCase
{
    Task<ResponseMonthlyDashboardJson> Execute(int year, int month);
}
