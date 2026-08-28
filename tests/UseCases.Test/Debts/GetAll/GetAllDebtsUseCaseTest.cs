using Balance.Application.UseCases.Debts.GetAll;
using Balance.Domain.Entities;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Debts.GetAll;

public class GetAllDebtsUseCaseTest
{
    [Fact]
    public async Task Success_Returns_The_Logged_Users_Debts()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt();

        scenario.DebtRepository.GetAll(scenario.User, null, null, false, [debt]);

        var result = await scenario.UseCase().Execute(null, null, false);

        result.Debts.Count.ShouldBe(1);
        result.Debts[0].Id.ShouldBe(debt.Id);
        result.Debts[0].CreditorName.ShouldBe(scenario.Creditor.Name);
        result.Debts[0].CategoryName.ShouldBe(scenario.Category.Name);
    }

    [Fact]
    public async Task Success_Filters_By_Creditor()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt();

        scenario.DebtRepository.GetAll(scenario.User, scenario.Creditor.Id, null, false, [debt]);

        var result = await scenario.UseCase().Execute(scenario.Creditor.Id, null, false);

        result.Debts.Select(d => d.Id).ShouldBe([debt.Id]);
    }

    [Fact]
    public async Task Success_Filters_By_Person()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt();

        scenario.DebtRepository.GetAll(scenario.User, null, scenario.Person.Id, false, [debt]);

        var result = await scenario.UseCase().Execute(null, scenario.Person.Id, false);

        result.Debts.Select(d => d.Id).ShouldBe([debt.Id]);
    }

    [Fact]
    public async Task Success_Excludes_An_Archived_Debt_By_Default_And_Includes_It_With_The_Flag()
    {
        var scenario = Scenario.Build();
        var activeDebt = scenario.Debt(archived: false);
        var archivedDebt = scenario.Debt(archived: true);

        scenario.DebtRepository.GetAll(scenario.User, null, null, false, [activeDebt]);
        scenario.DebtRepository.GetAll(scenario.User, null, null, true, [activeDebt, archivedDebt]);

        var defaultResult = await scenario.UseCase().Execute(null, null, false);
        var inclusiveResult = await scenario.UseCase().Execute(null, null, true);

        defaultResult.Debts.Select(d => d.Id).ShouldBe([activeDebt.Id]);
        inclusiveResult.Debts.Select(d => d.Id).ToHashSet().ShouldBe(new HashSet<Guid> { activeDebt.Id, archivedDebt.Id });
    }

    [Fact]
    public async Task Success_Excludes_A_Settled_Debt_By_Default_And_Includes_It_With_The_Flag()
    {
        var scenario = Scenario.Build();
        var unsettledDebt = scenario.Debt(totalAmount: 500m);
        var settledDebt = scenario.Debt(totalAmount: 500m);
        settledDebt.Payments = [DebtPaymentBuilder.Build(settledDebt, amountPaid: 500m)];

        // The repository cannot exclude a settled debt in SQL - it is derived from payments -
        // so both calls return the same raw list regardless of includeInactive.
        scenario.DebtRepository.GetAll(scenario.User, null, null, false, [unsettledDebt, settledDebt]);
        scenario.DebtRepository.GetAll(scenario.User, null, null, true, [unsettledDebt, settledDebt]);

        var defaultResult = await scenario.UseCase().Execute(null, null, false);
        var inclusiveResult = await scenario.UseCase().Execute(null, null, true);

        defaultResult.Debts.Select(d => d.Id).ShouldBe([unsettledDebt.Id]);
        inclusiveResult.Debts.Select(d => d.Id).ToHashSet().ShouldBe(new HashSet<Guid> { unsettledDebt.Id, settledDebt.Id });
    }

    [Fact]
    public async Task Success_An_Unsettled_And_An_Archived_Debt_Of_The_Same_Creditor_Are_Told_Apart()
    {
        var scenario = Scenario.Build();
        var unsettledDebt = scenario.Debt(archived: false);
        var archivedDebt = scenario.Debt(archived: true);

        scenario.DebtRepository.GetAll(scenario.User, scenario.Creditor.Id, null, false, [unsettledDebt]);
        scenario.DebtRepository.GetAll(
            scenario.User, scenario.Creditor.Id, null, true, [unsettledDebt, archivedDebt]);

        var defaultResult = await scenario.UseCase().Execute(scenario.Creditor.Id, null, false);
        var inclusiveResult = await scenario.UseCase().Execute(scenario.Creditor.Id, null, true);

        defaultResult.Debts.Count.ShouldBe(1);
        defaultResult.Debts[0].Id.ShouldBe(unsettledDebt.Id);
        defaultResult.Debts[0].Archived.ShouldBeFalse();

        inclusiveResult.Debts.Count.ShouldBe(2);
        inclusiveResult.Debts.Single(d => d.Id == archivedDebt.Id).Archived.ShouldBeTrue();
        inclusiveResult.Debts.Single(d => d.Id == unsettledDebt.Id).Archived.ShouldBeFalse();
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

        public DebtReadOnlyRepositoryBuilder DebtRepository { get; } = new();

        public static Scenario Build()
        {
            var user = UserBuilder.Build();

            return new Scenario
            {
                User = user,
                Person = PersonBuilder.Build(user),
                Creditor = CreditorBuilder.Build(user),
                Category = CategoryBuilder.Build(user)
            };
        }

        public Debt Debt(decimal? totalAmount = null, bool archived = false)
        {
            var debt = DebtBuilder.Build(Person, Creditor, Category, archived: archived);

            if (totalAmount.HasValue)
            {
                debt.TotalAmount = totalAmount.Value;
            }

            return debt;
        }

        public GetAllDebtsUseCase UseCase() =>
            new(DebtRepository.Build(), LoggedUserBuilder.Build(User));
    }
}
