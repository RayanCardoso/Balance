using Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;
using Balance.Communication.Requests;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Expenses.RegisterInstallmentPlan;

public class RegisterInstallmentPlanUseCaseTest
{
    [Fact]
    public async Task Creates_Exactly_N_Installments_Numbered_One_To_N_Referencing_The_Plan()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.InstallmentCount = 5;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Count.ShouldBe(5);
        result.Installments.Select(installment => installment.InstallmentNumber)
            .ShouldBe([1, 2, 3, 4, 5]);

        var added = scenario.ExpenseRepository.AddedRange;
        added.Count.ShouldBe(5);
        added.Select(installment => installment.InstallmentNumber).ShouldBe([1, 2, 3, 4, 5]);
        added.ShouldAllBe(installment => installment.InstallmentPlanId == result.Id);
    }

    [Fact]
    public async Task One_Hundred_Over_Three_Splits_Into_33_33_33_33_And_33_34()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.TotalAmount = 100.00m;
        request.InstallmentCount = 3;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Select(installment => installment.Amount)
            .ShouldBe([33.33m, 33.33m, 33.34m]);
    }

    [Theory]
    [InlineData(100.00, 3)]
    [InlineData(10.00, 3)]
    [InlineData(0.05, 3)]
    [InlineData(1000.00, 7)]
    [InlineData(99.99, 7)]
    [InlineData(1234.56, 11)]
    [InlineData(0.02, 12)]
    public async Task Installment_Amounts_Sum_To_The_Total_Exactly(decimal total, int count)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.TotalAmount = total;
        request.InstallmentCount = count;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Count.ShouldBe(count);
        result.Installments.Sum(installment => installment.Amount).ShouldBe(total);
    }

    [Theory]
    [InlineData(100.00, 3)]
    [InlineData(0.05, 3)]
    [InlineData(1000.00, 7)]
    public async Task The_Remainder_Lands_Entirely_On_The_Last_Installment(decimal total, int count)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.TotalAmount = total;
        request.InstallmentCount = count;

        var result = await scenario.UseCase().Execute(request);

        var each = Math.Round(total / count, 2, MidpointRounding.AwayFromZero);

        result.Installments.Take(count - 1).ShouldAllBe(installment => installment.Amount == each);
        result.Installments[count - 1].Amount.ShouldBe(total - each * (count - 1));
    }

    /// <summary>
    /// Pins the midpoint rounding mode with literal expected values. The test above recomputes its
    /// expectation with the implementation's own <c>Math.Round</c> call, so it follows the code
    /// wherever it goes and asserts nothing about which way a half-cent breaks. 0.05 over 2 is an
    /// exact midpoint - 0.025 - and is the smallest input that separates the two modes:
    /// AwayFromZero gives 0.03, ToEven gives 0.02.
    /// </summary>
    [Fact]
    public async Task An_Exact_Half_Cent_Rounds_Away_From_Zero_Not_To_Even()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.TotalAmount = 0.05m;
        request.InstallmentCount = 2;

        var result = await scenario.UseCase().Execute(request);

        result.Installments[0].Amount.ShouldBe(0.03m);
        result.Installments[1].Amount.ShouldBe(0.02m);
        result.Installments.Sum(installment => installment.Amount).ShouldBe(0.05m);
    }

    [Fact]
    public async Task Competence_Months_Advance_By_One_Month_From_The_Resolved_First_Month()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        // Past the closing day, so installment 1 lands on September.
        request.StartDate = new DateOnly(2026, 8, 21);
        request.InstallmentCount = 3;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Select(installment => installment.CompetenceMonth)
            .ShouldBe([new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1)]);
    }

    [Fact]
    public async Task Competence_Months_Cross_A_Year_Boundary()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.StartDate = new DateOnly(2026, 11, 5);
        request.InstallmentCount = 4;

        var result = await scenario.UseCase().Execute(request);

        result.Installments.Select(installment => installment.CompetenceMonth)
            .ShouldBe([
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 12, 1),
                new DateOnly(2027, 1, 1),
                new DateOnly(2027, 2, 1)
            ]);
    }

    [Fact]
    public async Task Every_Generated_Expense_Is_Credit_Dated_On_The_Start_Date_And_Inherits_The_Plans_Owners()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.StartDate = new DateOnly(2026, 8, 21);
        request.InstallmentCount = 4;

        await scenario.UseCase().Execute(request);

        var added = scenario.ExpenseRepository.AddedRange;

        added.ShouldAllBe(installment => installment.Type == ExpenseType.Credit);
        added.ShouldAllBe(installment => installment.Date == new DateOnly(2026, 8, 21));
        added.ShouldAllBe(installment => installment.PersonId == scenario.Person.Id);
        added.ShouldAllBe(installment => installment.CategoryId == scenario.Category.Id);
        added.ShouldAllBe(installment => installment.AccountId == scenario.Account.Id);
    }

    [Fact]
    public async Task The_Plans_End_Date_Is_The_Competence_Month_Of_The_Last_Installment()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.StartDate = new DateOnly(2026, 11, 21);
        request.InstallmentCount = 3;

        var result = await scenario.UseCase().Execute(request);

        // Installment 1 rolls to December; the last one is February of the next year.
        result.EndDate.ShouldBe(new DateOnly(2027, 2, 1));
        result.EndDate.ShouldBe(result.Installments[^1].CompetenceMonth);
        scenario.PlanRepository.Added!.EndDate.ShouldBe(new DateOnly(2027, 2, 1));
    }

    [Fact]
    public async Task The_Plan_And_Its_Installments_Are_Persisted_In_A_Single_Commit()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.InstallmentCount = 6;

        await scenario.UseCase().Execute(request);

        scenario.PlanRepository.Added.ShouldNotBeNull();
        scenario.ExpenseRepository.AddedRange.Count.ShouldBe(6);
        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Installment_Count_Below_Two(int count)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.InstallmentCount = count;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.INSTALLMENT_COUNT_INVALID);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Total_Amount_Not_Greater_Than_Zero(decimal total)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.TotalAmount = total;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    [Fact]
    public async Task Error_Person_Of_Another_User()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.PersonId = Guid.NewGuid();

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.PERSON_NOT_FOUND);
    }

    [Fact]
    public async Task Error_Category_Of_Another_User()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.CategoryId = Guid.NewGuid();

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.CATEGORY_NOT_FOUND);
    }

    [Fact]
    public async Task Error_Account_Of_Another_User()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.AccountId = Guid.NewGuid();

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.ACCOUNT_NOT_FOUND);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Category Category { get; init; }
        public required Account Account { get; init; }

        public InstallmentPlanWriteOnlyRepositoryBuilder PlanRepository { get; } = new();
        public ExpenseWriteOnlyRepositoryBuilder ExpenseRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build(int? closingDay = 20)
        {
            var user = UserBuilder.Build();
            var person = PersonBuilder.Build(user);
            var account = AccountBuilder.Build(person);
            account.ClosingDay = closingDay;

            return new Scenario
            {
                User = user,
                Person = person,
                Category = CategoryBuilder.Build(user),
                Account = account
            };
        }

        public RequestRegisterInstallmentPlanJson Request() =>
            RequestRegisterInstallmentPlanJsonBuilder.Build(Person.Id, Category.Id, Account.Id);

        public RegisterInstallmentPlanUseCase UseCase() =>
            new(
                PlanRepository.Build(),
                ExpenseRepository.Build(),
                new PersonReadOnlyRepositoryBuilder().GetById(User, Person).Build(),
                new CategoryReadOnlyRepositoryBuilder().GetById(User, Category).Build(),
                new AccountReadOnlyRepositoryBuilder().GetById(User, Account).Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
