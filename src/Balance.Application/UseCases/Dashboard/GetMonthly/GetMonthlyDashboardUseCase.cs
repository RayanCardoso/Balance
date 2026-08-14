using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Dashboard.GetMonthly;

/// <summary>
/// Composition only. The income half is produced by invoking the existing monthly income use case
/// through its published interface - no income type is read, reimplemented or modified here
/// (project decision AD-006).
/// </summary>
public class GetMonthlyDashboardUseCase : IGetMonthlyDashboardUseCase
{
    private readonly IGetMonthlyIncomeUseCase _getMonthlyIncomeUseCase;
    private readonly IGetMonthlyExpenseUseCase _getMonthlyExpenseUseCase;

    public GetMonthlyDashboardUseCase(
        IGetMonthlyIncomeUseCase getMonthlyIncomeUseCase,
        IGetMonthlyExpenseUseCase getMonthlyExpenseUseCase)
    {
        _getMonthlyIncomeUseCase = getMonthlyIncomeUseCase;
        _getMonthlyExpenseUseCase = getMonthlyExpenseUseCase;
    }

    public async Task<ResponseMonthlyDashboardJson> Execute(int year, int month)
    {
        var expenses = await _getMonthlyExpenseUseCase.Execute(year, month);
        var income = await _getMonthlyIncomeUseCase.Execute(year, month);

        return new ResponseMonthlyDashboardJson
        {
            CompetenceMonth = expenses.CompetenceMonth,
            Income = income,
            Expenses = expenses,
            Balance = income.TotalReceived - expenses.TotalCommitted
        };
    }
}
