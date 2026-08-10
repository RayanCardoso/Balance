using Balance.Application.UseCases.Incomes.GetMonthly;
using Balance.Communication.Enums;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Incomes.GetMonthly;

public class GetMonthlyIncomeUseCaseTest
{
    private static readonly DateOnly March = new(2026, 3, 1);

    [Fact]
    public async Task Recurring_Paid_In_Full_Is_Received()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), amount: 5000m, expectedDay: 5);
        AddPayment(source, 5000m);

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ExpectedAmount.ShouldBe(5000m);
        line.ExpectedDay.ShouldBe(5);
        line.ReceivedAmount.ShouldBe(5000m);
        line.Status.ShouldBe(IncomeStatus.Received);
    }

    [Fact]
    public async Task Recurring_Unpaid_Is_Pending_With_Zero_Received()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), amount: 4200m);

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ExpectedAmount.ShouldBe(4200m);
        line.ReceivedAmount.ShouldBe(0m);
        line.Status.ShouldBe(IncomeStatus.Pending);
    }

    [Fact]
    public async Task Recurring_Partially_Paid_Is_Divergent()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), amount: 5000m);
        AddPayment(source, 3000m);

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ReceivedAmount.ShouldBe(3000m);
        line.Status.ShouldBe(IncomeStatus.Divergent);
    }

    [Fact]
    public async Task Multiple_Payments_In_The_Month_Are_Summed()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), amount: 5000m);
        AddPayment(source, 2000m);
        AddPayment(source, 3000m);

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ReceivedAmount.ShouldBe(5000m);
        line.Status.ShouldBe(IncomeStatus.Received);
    }

    [Fact]
    public async Task Variable_With_Payments_Reports_Null_Expected_And_Received()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Variable(PersonBuilder.Build(user));
        AddPayment(source, 1800m);

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ExpectedAmount.ShouldBeNull();
        line.ExpectedDay.ShouldBeNull();
        line.ReceivedAmount.ShouldBe(1800m);
        line.Status.ShouldBe(IncomeStatus.Received);
    }

    [Fact]
    public async Task Variable_Without_Payments_Is_Pending()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Variable(PersonBuilder.Build(user));

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ExpectedAmount.ShouldBeNull();
        line.Status.ShouldBe(IncomeStatus.Pending);
    }

    [Fact]
    public async Task Totals_Are_The_Sum_Of_The_Lines()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);

        var salary = IncomeSourceBuilder.Recurring(person, amount: 5000m, name: "Salary");
        AddPayment(salary, 5000m);

        var rent = IncomeSourceBuilder.Recurring(person, amount: 1200m, name: "Rent");

        var freelance = IncomeSourceBuilder.Variable(person);
        AddPayment(freelance, 800m);

        var result = await CreateUseCase(user, [salary, rent, freelance]).Execute(2026, 3);

        result.Lines.Count.ShouldBe(3);
        result.TotalExpected.ShouldBe(6200m);
        result.TotalReceived.ShouldBe(5800m);
        result.ReferenceMonth.ShouldBe(March);
    }

    [Fact]
    public async Task A_Month_Inside_A_Closed_Version_Reports_That_Versions_Amount()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(
            PersonBuilder.Build(user), amount: 3000m, validityStart: new DateOnly(2026, 1, 1));

        // The first version was closed at the end of June; a raise starts in July.
        source.Versions[0].ValidityEnd = new DateOnly(2026, 6, 30);
        source.Versions.Add(new IncomeSourceVersion
        {
            Id = Guid.NewGuid(),
            IncomeSourceId = source.Id,
            Amount = 4500m,
            ExpectedDay = 5,
            ValidityStart = new DateOnly(2026, 7, 1),
            ValidityEnd = null,
            ChangeReason = "promotion"
        });

        var beforeRaise = await CreateUseCase(user, [source]).Execute(2026, 3);
        var afterRaise = await CreateUseCase(user, [source], new DateOnly(2026, 8, 1)).Execute(2026, 8);

        beforeRaise.Lines.ShouldHaveSingleItem().ExpectedAmount.ShouldBe(3000m);
        afterRaise.Lines.ShouldHaveSingleItem().ExpectedAmount.ShouldBe(4500m);
    }

    [Fact]
    public async Task A_Month_Before_Every_Version_Reports_Null_Expected()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(
            PersonBuilder.Build(user), amount: 3000m, validityStart: new DateOnly(2026, 6, 1));

        var result = await CreateUseCase(user, [source]).Execute(2026, 3);

        result.Lines.ShouldHaveSingleItem().ExpectedAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Empty_Account_Returns_No_Lines_And_Zero_Totals()
    {
        var user = UserBuilder.Build();

        var result = await CreateUseCase(user, []).Execute(2026, 3);

        result.Lines.ShouldBeEmpty();
        result.TotalExpected.ShouldBe(0m);
        result.TotalReceived.ShouldBe(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task Error_Reference_Month_Invalid(int month)
    {
        var user = UserBuilder.Build();

        var act = async () => await CreateUseCase(user, []).Execute(2026, month);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.REFERENCE_MONTH_INVALID);
    }

    private static void AddPayment(IncomeSource source, decimal amount)
    {
        source.Payments.Add(new IncomePayment
        {
            Id = Guid.NewGuid(),
            IncomeSourceId = source.Id,
            AmountReceived = amount,
            ReferenceMonth = March,
            PaymentDate = March.AddDays(4)
        });
    }

    private static GetMonthlyIncomeUseCase CreateUseCase(
        User user, List<IncomeSource> sources, DateOnly? referenceMonth = null)
    {
        var repository = new IncomeSourceReadOnlyRepositoryBuilder()
            .GetForMonth(user, referenceMonth ?? March, sources)
            .Build();

        return new GetMonthlyIncomeUseCase(repository, LoggedUserBuilder.Build(user));
    }
}
