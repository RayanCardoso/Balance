using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Shouldly;

namespace UseCases.Test.Expenses;

public class CompetenceMonthResolverTest
{
    [Fact]
    public void Credit_Before_The_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var result = CompetenceMonthResolver.Resolve(ExpenseType.Credit, closingDay: 20, new DateOnly(2026, 8, 15));

        result.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Credit_On_The_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var result = CompetenceMonthResolver.Resolve(ExpenseType.Credit, closingDay: 20, new DateOnly(2026, 8, 20));

        result.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Credit_After_The_Closing_Day_Rolls_To_The_Following_Month()
    {
        var result = CompetenceMonthResolver.Resolve(ExpenseType.Credit, closingDay: 20, new DateOnly(2026, 8, 21));

        result.ShouldBe(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void Credit_In_December_After_The_Closing_Day_Rolls_Into_January_Of_The_Next_Year()
    {
        var result = CompetenceMonthResolver.Resolve(ExpenseType.Credit, closingDay: 20, new DateOnly(2026, 12, 21));

        result.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void Credit_On_An_Account_With_No_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var result = CompetenceMonthResolver.Resolve(ExpenseType.Credit, closingDay: null, new DateOnly(2026, 8, 28));

        result.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Theory]
    [InlineData(ExpenseType.Debit)]
    [InlineData(ExpenseType.Pix)]
    public void Debit_And_Pix_Ignore_The_Closing_Day(ExpenseType type)
    {
        var result = CompetenceMonthResolver.Resolve(type, closingDay: 20, new DateOnly(2026, 8, 28));

        result.ShouldBe(new DateOnly(2026, 8, 1));
    }
}
