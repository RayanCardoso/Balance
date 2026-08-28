using System.Globalization;
using Balance.Application.UseCases.Creditors.GetSummary;
using Balance.Domain.Entities;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Creditors.GetSummary;

public class GetCreditorSummaryUseCaseTest
{
    // Message assertions pin literal text (L-010), which requires a fixed culture rather than
    // whatever the host machine's ambient thread culture happens to be.
    private static async Task WithInvariantCulture(Func<Task> action)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task Success_One_Settled_And_One_Unsettled_Debt_Report_Only_The_Unsettled_Ones_Figures()
    {
        var scenario = Scenario.Build();

        var unsettledDebt = scenario.Debt(totalAmount: 1000m);
        unsettledDebt.Payments = [DebtPaymentBuilder.Build(unsettledDebt, amountPaid: 200m)];

        var settledDebt = scenario.Debt(totalAmount: 500m);
        settledDebt.Payments = [DebtPaymentBuilder.Build(settledDebt, amountPaid: 500m)];

        scenario.DebtRepository.GetByCreditor(scenario.User, scenario.Creditor.Id, [unsettledDebt, settledDebt]);

        var result = await scenario.UseCase().Execute(scenario.Creditor.Id);

        result.UnsettledDebtCount.ShouldBe(1);
        result.TotalOwed.ShouldBe(1000m);
        result.TotalPaid.ShouldBe(200m);
        result.OutstandingBalance.ShouldBe(800m);
        result.Creditor.Id.ShouldBe(scenario.Creditor.Id);
        result.Creditor.Name.ShouldBe(scenario.Creditor.Name);
    }

    [Fact]
    public async Task Success_A_Creditor_With_No_Debts_Returns_Zeroes_Rather_Than_Null()
    {
        var scenario = Scenario.Build();

        scenario.DebtRepository.GetByCreditor(scenario.User, scenario.Creditor.Id, []);

        var result = await scenario.UseCase().Execute(scenario.Creditor.Id);

        result.ShouldNotBeNull();
        result.Creditor.ShouldNotBeNull();
        result.UnsettledDebtCount.ShouldBe(0);
        result.TotalOwed.ShouldBe(0m);
        result.TotalPaid.ShouldBe(0m);
        result.OutstandingBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task Success_An_Archived_Debt_Is_Excluded()
    {
        var scenario = Scenario.Build();

        var archivedDebt = scenario.Debt(totalAmount: 700m, archived: true);
        var openDebt = scenario.Debt(totalAmount: 300m);

        scenario.DebtRepository.GetByCreditor(scenario.User, scenario.Creditor.Id, [archivedDebt, openDebt]);

        var result = await scenario.UseCase().Execute(scenario.Creditor.Id);

        result.UnsettledDebtCount.ShouldBe(1);
        result.TotalOwed.ShouldBe(300m);
        result.TotalPaid.ShouldBe(0m);
        result.OutstandingBalance.ShouldBe(300m);
    }

    [Fact]
    public async Task Error_A_Foreign_Creditor_Throws_Not_Found()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var act = async () => await scenario.UseCase().Execute(Guid.NewGuid());

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Creditor not found.");
        });
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

        public CreditorReadOnlyRepositoryBuilder CreditorRepository { get; } = new();
        public DebtReadOnlyRepositoryBuilder DebtRepository { get; } = new();

        public static Scenario Build()
        {
            var user = UserBuilder.Build();
            var creditor = CreditorBuilder.Build(user);

            var scenario = new Scenario
            {
                User = user,
                Person = PersonBuilder.Build(user),
                Creditor = creditor,
                Category = CategoryBuilder.Build(user)
            };

            scenario.CreditorRepository.GetById(user, creditor);

            return scenario;
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

        public GetCreditorSummaryUseCase UseCase() =>
            new(CreditorRepository.Build(), DebtRepository.Build(), LoggedUserBuilder.Build(User));
    }
}
