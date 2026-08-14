using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Shouldly;

namespace UseCases.Test.RecurringExpenses;

public class RecurringExpenseExtensionsTest
{
    private static readonly DateOnly March = new(2026, 3, 1);

    [Fact]
    public void A_Month_Before_Every_Version_Has_No_Version_In_Effect()
    {
        var expense = Build((150m, new DateOnly(2026, 6, 1), null));

        expense.VersionInEffect(March).ShouldBeNull();
    }

    [Fact]
    public void A_Month_Inside_A_Closed_Versions_Range_Returns_That_Closed_Version()
    {
        var expense = Build(
            (150m, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            (180m, new DateOnly(2026, 7, 1), null));

        expense.VersionInEffect(March)!.Amount.ShouldBe(150m);
    }

    [Fact]
    public void A_Month_After_A_Closed_Version_Returns_The_Open_One()
    {
        var expense = Build(
            (150m, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            (180m, new DateOnly(2026, 7, 1), null));

        expense.VersionInEffect(new DateOnly(2026, 8, 1))!.Amount.ShouldBe(180m);
    }

    [Fact]
    public void Two_Versions_Overlapping_The_Same_Month_Return_The_One_With_The_Later_Start()
    {
        // The old version runs until the 14th of March, the new one from the 15th.
        // Both satisfy the range test for March, so only the ordering separates them.
        var expense = Build(
            (150m, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 14)),
            (180m, new DateOnly(2026, 3, 15), null));

        expense.VersionInEffect(March)!.Amount.ShouldBe(180m);
    }

    [Fact]
    public void A_Version_Starting_On_The_Last_Day_Of_The_Month_Is_In_Effect()
    {
        var expense = Build((180m, new DateOnly(2026, 3, 31), null));

        expense.VersionInEffect(March)!.Amount.ShouldBe(180m);
    }

    [Fact]
    public void A_Version_Ending_On_The_First_Day_Of_The_Month_Is_In_Effect()
    {
        var expense = Build((150m, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1)));

        expense.VersionInEffect(March)!.Amount.ShouldBe(150m);
    }

    [Fact]
    public void A_Month_After_Every_Version_Was_Closed_Has_No_Version_In_Effect()
    {
        var expense = Build((150m, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28)));

        expense.VersionInEffect(March).ShouldBeNull();
    }

    private static RecurringExpense Build(params (decimal Amount, DateOnly Start, DateOnly? End)[] versions)
    {
        var expense = new RecurringExpense { Name = "Luz", DueDay = 10 };

        foreach (var (amount, start, end) in versions)
        {
            expense.Versions.Add(new RecurringExpenseVersion
            {
                RecurringExpenseId = expense.Id,
                Amount = amount,
                ValidityStart = start,
                ValidityEnd = end,
                ChangeReason = "initial"
            });
        }

        return expense;
    }
}
