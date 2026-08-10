using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Incomes.GetMonthly;

public interface IGetMonthlyIncomeUseCase
{
    Task<ResponseMonthlyIncomeJson> Execute(int year, int month);
}
