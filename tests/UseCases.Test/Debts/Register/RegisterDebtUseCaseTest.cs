using System.Globalization;
using Balance.Application.UseCases.Debts.Register;
using Balance.Communication.Requests;
using Balance.Domain.Entities;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Debts.Register;

public class RegisterDebtUseCaseTest
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
    public async Task Scheduled_1500_Over_10_From_March_20_Due_Day_10_Produces_Expected_Schedule()
    {
        var scenario = Scenario.Build();
        var request = scenario.ScheduledRequest();
        request.PrincipalAmount = 1500.00m;
        request.TotalAmount = 1500.00m;
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 10;
        request.InstallmentCount = 10;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Count.ShouldBe(10);
        result.Installments.ShouldAllBe(installment => installment.ExpectedAmount == 150.00m);

        result.Installments[0].ReferenceMonth.ShouldBe(new DateOnly(2026, 4, 1));
        result.Installments[0].DueDate.ShouldBe(new DateOnly(2026, 4, 10));
        result.Installments[^1].ReferenceMonth.ShouldBe(new DateOnly(2027, 1, 1));
        result.Installments[^1].DueDate.ShouldBe(new DateOnly(2027, 1, 10));

        result.EndMonth.ShouldBe(new DateOnly(2027, 1, 1));
        result.EndMonth.ShouldBe(result.Installments[^1].ReferenceMonth);

        scenario.DebtRepository.Added!.EndMonth.ShouldBe(new DateOnly(2027, 1, 1));
        scenario.InstallmentRepository.AddedRange.Count.ShouldBe(10);
    }

    [Fact]
    public async Task Total_1000_Over_3_Splits_Into_333_33_333_33_333_34()
    {
        var scenario = Scenario.Build();
        var request = scenario.ScheduledRequest();
        request.PrincipalAmount = 1000.00m;
        request.TotalAmount = 1000.00m;
        request.InstallmentCount = 3;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Select(installment => installment.ExpectedAmount)
            .ShouldBe([333.33m, 333.33m, 333.34m]);
        result.Installments.Sum(installment => installment.ExpectedAmount).ShouldBe(1000.00m);
    }

    [Fact]
    public async Task Same_Start_Date_With_Due_Day_25_Puts_Installment_One_In_March()
    {
        var scenario = Scenario.Build();
        var request = scenario.ScheduledRequest();
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 25;
        request.InstallmentCount = 1;

        var result = await scenario.UseCase().Execute(request);

        result.Installments[0].ReferenceMonth.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task Due_Day_31_Gives_The_February_Installment_A_Due_Date_Of_The_28th()
    {
        var scenario = Scenario.Build();
        var request = scenario.ScheduledRequest();
        request.StartDate = new DateOnly(2026, 1, 31);
        request.DueDay = 31;
        request.InstallmentCount = 2;

        var result = await scenario.UseCase().Execute(request);

        result.Installments[0].ReferenceMonth.ShouldBe(new DateOnly(2026, 1, 1));
        result.Installments[0].DueDate.ShouldBe(new DateOnly(2026, 1, 31));
        result.Installments[1].ReferenceMonth.ShouldBe(new DateOnly(2026, 2, 1));
        result.Installments[1].DueDate.ShouldBe(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task OpenEnded_Persists_Zero_Installments_And_Null_Schedule_Fields()
    {
        var scenario = Scenario.Build();
        var request = scenario.OpenEndedRequest();

        var result = await scenario.UseCase().Execute(request);

        result.Installments.ShouldBeEmpty();
        result.DueDay.ShouldBeNull();
        result.InstallmentCount.ShouldBeNull();
        result.EndMonth.ShouldBeNull();

        scenario.DebtRepository.Added.ShouldNotBeNull();
        scenario.DebtRepository.Added!.DueDay.ShouldBeNull();
        scenario.DebtRepository.Added!.InstallmentCount.ShouldBeNull();
        scenario.DebtRepository.Added!.EndMonth.ShouldBeNull();
        scenario.InstallmentRepository.AddedRange.ShouldBeEmpty();
    }

    [Fact]
    public async Task Error_Foreign_Creditor()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var request = scenario.ScheduledRequest();
            request.CreditorId = Guid.NewGuid();

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Creditor not found.");
        });
    }

    [Fact]
    public async Task Error_Foreign_Person()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var request = scenario.ScheduledRequest();
            request.PersonId = Guid.NewGuid();

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Person not found.");
        });
    }

    [Fact]
    public async Task Error_Foreign_Category()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var request = scenario.ScheduledRequest();
            request.CategoryId = Guid.NewGuid();

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Category not found.");
        });
    }

    [Fact]
    public async Task Commit_Is_Called_Exactly_Once()
    {
        var scenario = Scenario.Build();
        var request = scenario.ScheduledRequest();
        request.InstallmentCount = 6;

        await scenario.UseCase().Execute(request);

        scenario.DebtRepository.Added.ShouldNotBeNull();
        scenario.InstallmentRepository.AddedRange.Count.ShouldBe(6);
        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Category Category { get; init; }
        public required Creditor Creditor { get; init; }

        public DebtWriteOnlyRepositoryBuilder DebtRepository { get; } = new();
        public DebtInstallmentWriteOnlyRepositoryBuilder InstallmentRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build()
        {
            var user = UserBuilder.Build();

            return new Scenario
            {
                User = user,
                Person = PersonBuilder.Build(user),
                Category = CategoryBuilder.Build(user),
                Creditor = CreditorBuilder.Build(user)
            };
        }

        public RequestRegisterDebtJson ScheduledRequest()
        {
            var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
            request.CreditorId = Creditor.Id;
            request.PersonId = Person.Id;
            request.CategoryId = Category.Id;

            return request;
        }

        public RequestRegisterDebtJson OpenEndedRequest()
        {
            var request = RequestRegisterDebtJsonBuilder.BuildOpenEnded();
            request.CreditorId = Creditor.Id;
            request.PersonId = Person.Id;
            request.CategoryId = Category.Id;

            return request;
        }

        public RegisterDebtUseCase UseCase() =>
            new(
                DebtRepository.Build(),
                InstallmentRepository.Build(),
                new CreditorReadOnlyRepositoryBuilder().GetById(User, Creditor).Build(),
                new PersonReadOnlyRepositoryBuilder().GetById(User, Person).Build(),
                new CategoryReadOnlyRepositoryBuilder().GetById(User, Category).Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
