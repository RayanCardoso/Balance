using Balance.Application.UseCases.Debts.GetMonthly;
using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Communication.Responses;

namespace Balance.Application.UseCases.Dashboard.GetMonthly;

/// <summary>
/// Composition only. Each half is produced by invoking the existing monthly income, expense and debt
/// use cases through their published interfaces - no income, expense or debt type is read,
/// reimplemented or modified here (project decision AD-006).
/// </summary>
public class GetMonthlyDashboardUseCase : IGetMonthlyDashboardUseCase
{
    private readonly IGetMonthlyIncomeUseCase _getMonthlyIncomeUseCase;
    private readonly IGetMonthlyExpenseUseCase _getMonthlyExpenseUseCase;
    private readonly IGetMonthlyDebtUseCase _getMonthlyDebtUseCase;

    public GetMonthlyDashboardUseCase(
        IGetMonthlyIncomeUseCase getMonthlyIncomeUseCase,
        IGetMonthlyExpenseUseCase getMonthlyExpenseUseCase,
        IGetMonthlyDebtUseCase getMonthlyDebtUseCase)
    {
        _getMonthlyIncomeUseCase = getMonthlyIncomeUseCase;
        _getMonthlyExpenseUseCase = getMonthlyExpenseUseCase;
        _getMonthlyDebtUseCase = getMonthlyDebtUseCase;
    }

    public async Task<ResponseMonthlyDashboardJson> Execute(int year, int month)
    {
        var expenses = await _getMonthlyExpenseUseCase.Execute(year, month);
        var income = await _getMonthlyIncomeUseCase.Execute(year, month);
        var debts = await _getMonthlyDebtUseCase.Execute(year, month);

        return new ResponseMonthlyDashboardJson
        {
            CompetenceMonth = expenses.CompetenceMonth,
            Income = income,
            Expenses = expenses,
            Debts = debts,
            Balance = income.TotalReceived - expenses.TotalCommitted - debts.TotalCommitted
        };
    }
}
