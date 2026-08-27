using System.Globalization;
using Balance.Application.UseCases.Debts.Archive;
using Balance.Domain.Entities;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Debts.Archive;

public class ArchiveDebtUseCaseTest
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
    public async Task Archiving_Sets_The_Flag_And_Commits()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt(archived: false);
        var unitOfWork = new UnitOfWorkBuilder();

        await scenario.UseCase(debt, unitOfWork).Execute(debt.Id, archived: true);

        debt.Archived.ShouldBeTrue();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Unarchiving_Clears_The_Flag()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt(archived: true);
        var unitOfWork = new UnitOfWorkBuilder();

        await scenario.UseCase(debt, unitOfWork).Execute(debt.Id, archived: false);

        debt.Archived.ShouldBeFalse();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Error_A_Foreign_Id_Throws_Not_Found()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var unitOfWork = new UnitOfWorkBuilder();

            var useCase = new ArchiveDebtUseCase(
                new DebtUpdateOnlyRepositoryBuilder().Build(),
                unitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(scenario.User));

            var act = async () => await useCase.Execute(Guid.NewGuid(), archived: true);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Debt not found.");
            unitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task The_Debts_Payments_Are_Untouched_By_The_Call()
    {
        var scenario = Scenario.Build();
        var debt = scenario.Debt(archived: false);
        var payment = DebtPaymentBuilder.Build(debt, amountPaid: 250m);
        debt.Payments = [payment];
        var unitOfWork = new UnitOfWorkBuilder();

        await scenario.UseCase(debt, unitOfWork).Execute(debt.Id, archived: true);

        debt.Payments.Count.ShouldBe(1);
        debt.Payments[0].Id.ShouldBe(payment.Id);
        debt.Payments[0].AmountPaid.ShouldBe(250m);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

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

        public Debt Debt(bool archived) => DebtBuilder.Build(Person, Creditor, Category, archived: archived);

        public ArchiveDebtUseCase UseCase(Debt debt, UnitOfWorkBuilder unitOfWork) =>
            new(
                new DebtUpdateOnlyRepositoryBuilder().GetById(User, debt).Build(),
                unitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
