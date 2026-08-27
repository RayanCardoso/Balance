using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Shouldly;

namespace UseCases.Test.Domain;

public class DebtExtensionsTest
{
    private static Debt DebtOf(decimal totalAmount, params decimal[] amountsPaid) => new()
    {
        TotalAmount = totalAmount,
        Payments = amountsPaid.Select(amount => new DebtPayment { AmountPaid = amount }).ToList()
    };

    [Fact]
    public void A_Debt_With_No_Payments_Is_Outstanding_For_The_Full_Amount_And_Not_Settled()
    {
        var debt = DebtOf(1500.00m);

        debt.OutstandingBalance().ShouldBe(1500.00m);
        debt.IsSettled().ShouldBeFalse();
    }

    [Fact]
    public void Two_Partial_Payments_Reduce_The_Outstanding_Balance()
    {
        var debt = DebtOf(1500.00m, 150.00m, 150.00m);

        debt.OutstandingBalance().ShouldBe(1200.00m);
    }

    [Fact]
    public void Payments_Summing_Exactly_To_The_Total_Leave_A_Zero_Balance_And_Are_Settled()
    {
        var debt = DebtOf(1500.00m, 750.00m, 750.00m);

        debt.OutstandingBalance().ShouldBe(0m);
        debt.IsSettled().ShouldBeTrue();
    }

    [Fact]
    public void An_Overpayment_Leaves_A_Negative_Balance_And_Is_Settled()
    {
        var debt = DebtOf(1500.00m, 1000.00m, 700.00m);

        debt.OutstandingBalance().ShouldBe(-200.00m);
        debt.IsSettled().ShouldBeTrue();
    }

    [Fact]
    public void An_Empty_Payments_Collection_Does_Not_Throw()
    {
        var debt = new Debt { TotalAmount = 500.00m };

        Should.NotThrow(() => debt.OutstandingBalance());
        debt.OutstandingBalance().ShouldBe(500.00m);
    }
}
