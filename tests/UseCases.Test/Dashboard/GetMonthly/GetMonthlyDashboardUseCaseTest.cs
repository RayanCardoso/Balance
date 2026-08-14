using Balance.Application.UseCases.Dashboard.GetMonthly;
using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Communication.Responses;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using Moq;
using Shouldly;

namespace UseCases.Test.Dashboard.GetMonthly;

public class GetMonthlyDashboardUseCaseTest
{
    private static readonly DateOnly August = new(2026, 8, 1);

    [Fact]
    public async Task Both_Halves_Are_Returned_Untouched_From_Their_Use_Cases()
    {
        var income = NewIncome(totalExpected: 5000m, totalReceived: 4800m);
        var expenses = NewExpenses(totalCommitted: 1200m);

        var useCase = BuildUseCase(income, expenses);

        var result = await useCase.Execute(2026, 8);

        result.Income.ShouldBeSameAs(income);
        result.Expenses.ShouldBeSameAs(expenses);
        result.CompetenceMonth.ShouldBe(August);
    }

    [Fact]
    public async Task The_Balance_Is_The_Income_Received_Minus_The_Committed_Expense()
    {
        var useCase = BuildUseCase(
            NewIncome(totalExpected: 5000m, totalReceived: 4800m),
            NewExpenses(totalCommitted: 1200m));

        var result = await useCase.Execute(2026, 8);

        result.Balance.ShouldBe(3600m);
    }

    [Fact]
    public async Task The_Balance_Ignores_The_Expected_Income_And_Uses_Only_What_Arrived()
    {
        // Expected 5000 but only 1000 landed, against 1200 committed: the month is negative.
        var useCase = BuildUseCase(
            NewIncome(totalExpected: 5000m, totalReceived: 1000m),
            NewExpenses(totalCommitted: 1200m));

        var result = await useCase.Execute(2026, 8);

        result.Balance.ShouldBe(-200m);
    }

    [Fact]
    public async Task The_Balance_Uses_The_Committed_Total_Not_The_Amount_Actually_Paid()
    {
        // 900 already paid, 1200 committed once the unpaid estimates are counted.
        var expenses = NewExpenses(totalCommitted: 1200m);
        expenses.TotalRecurringPaid = 900m;

        var useCase = BuildUseCase(NewIncome(totalExpected: 5000m, totalReceived: 4800m), expenses);

        var result = await useCase.Execute(2026, 8);

        result.Balance.ShouldBe(3600m);
    }

    [Fact]
    public async Task Both_Use_Cases_Are_Invoked_For_The_Requested_Month()
    {
        var incomeUseCase = new Mock<IGetMonthlyIncomeUseCase>();
        var expenseUseCase = new Mock<IGetMonthlyExpenseUseCase>();

        incomeUseCase.Setup(u => u.Execute(2026, 8)).ReturnsAsync(NewIncome(0m, 0m));
        expenseUseCase.Setup(u => u.Execute(2026, 8)).ReturnsAsync(NewExpenses(0m));

        var useCase = new GetMonthlyDashboardUseCase(incomeUseCase.Object, expenseUseCase.Object);

        await useCase.Execute(2026, 8);

        incomeUseCase.Verify(u => u.Execute(2026, 8), Times.Once);
        expenseUseCase.Verify(u => u.Execute(2026, 8), Times.Once);
    }

    [Fact]
    public async Task An_Invalid_Month_Propagates_The_Validation_Error()
    {
        var expenseUseCase = new Mock<IGetMonthlyExpenseUseCase>();

        expenseUseCase
            .Setup(u => u.Execute(2026, 13))
            .ThrowsAsync(new ErrorOnValidationException([ResourceErrorMessages.REFERENCE_MONTH_INVALID]));

        var useCase = new GetMonthlyDashboardUseCase(
            new Mock<IGetMonthlyIncomeUseCase>().Object,
            expenseUseCase.Object);

        var exception = await Should.ThrowAsync<ErrorOnValidationException>(
            async () => await useCase.Execute(2026, 13));

        exception.GetErrors().ShouldHaveSingleItem()
            .ShouldBe(ResourceErrorMessages.REFERENCE_MONTH_INVALID);
    }

    private static ResponseMonthlyIncomeJson NewIncome(decimal totalExpected, decimal totalReceived) =>
        new()
        {
            ReferenceMonth = August,
            TotalExpected = totalExpected,
            TotalReceived = totalReceived
        };

    private static ResponseMonthlyExpenseJson NewExpenses(decimal totalCommitted) =>
        new()
        {
            CompetenceMonth = August,
            TotalCommitted = totalCommitted
        };

    private static GetMonthlyDashboardUseCase BuildUseCase(
        ResponseMonthlyIncomeJson income,
        ResponseMonthlyExpenseJson expenses)
    {
        var incomeUseCase = new Mock<IGetMonthlyIncomeUseCase>();
        var expenseUseCase = new Mock<IGetMonthlyExpenseUseCase>();

        incomeUseCase.Setup(u => u.Execute(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(income);
        expenseUseCase.Setup(u => u.Execute(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(expenses);

        return new GetMonthlyDashboardUseCase(incomeUseCase.Object, expenseUseCase.Object);
    }
}
