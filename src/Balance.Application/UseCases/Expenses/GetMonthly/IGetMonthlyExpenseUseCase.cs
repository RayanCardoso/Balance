using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Expenses.GetMonthly;

public interface IGetMonthlyExpenseUseCase
{
    Task<ResponseMonthlyExpenseJson> Execute(int year, int month);
}
