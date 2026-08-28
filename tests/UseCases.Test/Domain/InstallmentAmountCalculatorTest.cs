using Balance.Domain.Extensions;
using Shouldly;

namespace UseCases.Test.Domain;

public class InstallmentAmountCalculatorTest
{
    [Fact]
    public void One_Hundred_Over_Three_Splits_Into_33_33_33_33_And_33_34()
    {
        var result = InstallmentAmountCalculator.Split(100.00m, 3);

        result.ShouldSatisfyAllConditions(
            () => result.ShouldBe([33.33m, 33.33m, 33.34m]),
            () => result.Sum().ShouldBe(100.00m));
    }

    [Fact]
    public void Fifteen_Hundred_Over_Ten_Splits_Evenly_Into_Ten_Of_150()
    {
        var result = InstallmentAmountCalculator.Split(1500.00m, 10);

        result.ShouldSatisfyAllConditions(
            () => result.ShouldBe(Enumerable.Repeat(150.00m, 10).ToList()),
            () => result.Sum().ShouldBe(1500.00m));
    }

    [Fact]
    public void One_Thousand_Over_Seven_Sums_Exactly_To_The_Total()
    {
        var result = InstallmentAmountCalculator.Split(1000.00m, 7);

        result.Count.ShouldBe(7);
        result.Sum().ShouldBe(1000.00m);
    }

    [Fact]
    public void Five_Cents_Over_Three_Sums_Exactly_To_The_Total()
    {
        var result = InstallmentAmountCalculator.Split(0.05m, 3);

        result.Count.ShouldBe(3);
        result.Sum().ShouldBe(0.05m);
    }

    [Fact]
    public void Nine_Hundred_Ninety_Nine_Ninety_Nine_Over_Four_Sums_Exactly_To_The_Total()
    {
        var result = InstallmentAmountCalculator.Split(999.99m, 4);

        result.Count.ShouldBe(4);
        result.Sum().ShouldBe(999.99m);
    }

    [Fact]
    public void A_Single_Installment_Returns_The_Total_Unchanged()
    {
        var result = InstallmentAmountCalculator.Split(123.45m, 1);

        result.ShouldSatisfyAllConditions(
            () => result.ShouldBe([123.45m]),
            () => result.Sum().ShouldBe(123.45m));
    }

    /// <summary>
    /// Pins the midpoint rounding mode with literal expected values. 0.05 over 2 is an exact
    /// midpoint - 0.025 - and is the smallest input that separates the two modes: AwayFromZero
    /// gives 0.03, ToEven gives 0.02.
    /// </summary>
    [Fact]
    public void An_Exact_Half_Cent_Rounds_Away_From_Zero_Not_To_Even()
    {
        var result = InstallmentAmountCalculator.Split(0.05m, 2);

        result.ShouldSatisfyAllConditions(
            () => result.ShouldBe([0.03m, 0.02m]),
            () => result.Sum().ShouldBe(0.05m));
    }
}
