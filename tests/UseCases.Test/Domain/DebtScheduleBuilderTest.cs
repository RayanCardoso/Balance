using Balance.Domain.Extensions;
using Shouldly;

namespace UseCases.Test.Domain;

public class DebtScheduleBuilderTest
{
    [Fact]
    public void A_Start_Date_After_The_Due_Day_Rolls_To_The_Following_Month()
    {
        var result = DebtScheduleBuilder.FirstCompetenceMonth(new DateOnly(2026, 3, 20), 10);

        result.ShouldBe(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public void A_Start_Date_Before_The_Due_Day_Stays_In_The_Same_Month()
    {
        var result = DebtScheduleBuilder.FirstCompetenceMonth(new DateOnly(2026, 3, 5), 10);

        result.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void A_Start_Date_Equal_To_The_Due_Day_Stays_In_The_Same_Month()
    {
        var result = DebtScheduleBuilder.FirstCompetenceMonth(new DateOnly(2026, 3, 10), 10);

        result.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void A_Due_Day_Past_A_Short_Month_Clamps_To_The_Twenty_Eighth_Of_February()
    {
        var result = DebtScheduleBuilder.DueDateIn(new DateOnly(2026, 2, 1), 31);

        result.ShouldBe(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void A_Due_Day_Past_February_Clamps_To_The_Twenty_Ninth_On_A_Leap_Year()
    {
        var result = DebtScheduleBuilder.DueDateIn(new DateOnly(2024, 2, 1), 31);

        result.ShouldBe(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public void A_Due_Day_Of_Thirty_One_Clamps_To_The_Thirtieth_Of_April()
    {
        var result = DebtScheduleBuilder.DueDateIn(new DateOnly(2026, 4, 1), 31);

        result.ShouldBe(new DateOnly(2026, 4, 30));
    }

    [Fact]
    public void A_Due_Day_Within_The_Month_Is_Used_Unchanged()
    {
        var result = DebtScheduleBuilder.DueDateIn(new DateOnly(2026, 3, 1), 10);

        result.ShouldBe(new DateOnly(2026, 3, 10));
    }
}
