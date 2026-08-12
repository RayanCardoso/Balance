using Balance.Application.UseCases.RecurringExpenses.Register;
using Balance.Communication.Requests;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.RecurringExpenses.Register;

public class RegisterRecurringExpenseUseCaseTest
{
    [Fact]
    public async Task Success_Persists_The_Recurring_Expense_And_Its_First_Version()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.WriteRepository.Added.ShouldNotBeNull();

        added.Name.ShouldBe(request.Name);
        added.DueDay.ShouldBe(request.DueDay);
        added.PersonId.ShouldBe(scenario.Person.Id);
        added.CategoryId.ShouldBe(scenario.Category.Id);
        added.AccountId.ShouldBe(scenario.Account.Id);

        var version = scenario.WriteRepository.AddedVersions.ShouldHaveSingleItem();

        version.RecurringExpenseId.ShouldBe(added.Id);
        version.Amount.ShouldBe(request.Amount);
        version.ValidityStart.ShouldBe(request.ValidityStart!.Value);
        version.ChangeReason.ShouldBe(request.ChangeReason);

        result.Id.ShouldBe(added.Id);
        result.Name.ShouldBe(request.Name);
        result.DueDay.ShouldBe(request.DueDay);
        result.PersonId.ShouldBe(scenario.Person.Id);
        result.CategoryId.ShouldBe(scenario.Category.Id);
        result.AccountId.ShouldBe(scenario.Account.Id);
        result.Versions.ShouldHaveSingleItem().Amount.ShouldBe(request.Amount);
    }

    [Fact]
    public async Task The_First_Version_Is_Open()
    {
        var scenario = Scenario.Build();

        var result = await scenario.UseCase().Execute(scenario.Request());

        scenario.WriteRepository.AddedVersions.ShouldHaveSingleItem().ValidityEnd.ShouldBeNull();
        result.Versions.ShouldHaveSingleItem().ValidityEnd.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Archived_Is_False_And_The_Supplied_IsEstimate_Is_Stored(bool isEstimate)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.IsEstimate = isEstimate;

        var result = await scenario.UseCase().Execute(request);

        scenario.WriteRepository.Added!.Archived.ShouldBeFalse();
        scenario.WriteRepository.Added!.IsEstimate.ShouldBe(isEstimate);

        result.Archived.ShouldBeFalse();
        result.IsEstimate.ShouldBe(isEstimate);
    }

    [Fact]
    public async Task The_Expense_And_Its_Version_Are_Saved_In_One_Transaction()
    {
        var scenario = Scenario.Build();

        await scenario.UseCase().Execute(scenario.Request());

        scenario.UnitOfWork.Commits.ShouldBe(1);
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

    [Fact]
    public async Task Error_Name_Empty()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.Name = string.Empty;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.NAME_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Amount_Not_Greater_Than_Zero(decimal amount)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.Amount = amount;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Error_Due_Day_Out_Of_Range(int dueDay)
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();
        request.DueDay = dueDay;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Category Category { get; init; }
        public required Account Account { get; init; }

        public RecurringExpenseWriteOnlyRepositoryBuilder WriteRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build()
        {
            var user = UserBuilder.Build();
            var person = PersonBuilder.Build(user);

            return new Scenario
            {
                User = user,
                Person = person,
                Category = CategoryBuilder.Build(user),
                Account = AccountBuilder.Build(person)
            };
        }

        public RequestRegisterRecurringExpenseJson Request() =>
            RequestRegisterRecurringExpenseJsonBuilder.Build(Person.Id, Category.Id, Account.Id);

        public RegisterRecurringExpenseUseCase UseCase() =>
            new(
                WriteRepository.Build(),
                new PersonReadOnlyRepositoryBuilder().GetById(User, Person).Build(),
                new CategoryReadOnlyRepositoryBuilder().GetById(User, Category).Build(),
                new AccountReadOnlyRepositoryBuilder().GetById(User, Account).Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
