using Balance.Application.UseCases.Expenses.Register;
using Balance.Communication.Enums;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Expenses.Register;

public class RegisterExpenseUseCaseTest
{
    [Fact]
    public async Task Success_Persists_Every_Field()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request();

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.WriteRepository.Added.ShouldNotBeNull();

        added.Name.ShouldBe(request.Name);
        added.PersonId.ShouldBe(scenario.Person.Id);
        added.Type.ShouldBe(Balance.Domain.Enums.ExpenseType.Credit);
        added.Amount.ShouldBe(request.Amount);
        added.CategoryId.ShouldBe(scenario.Category.Id);
        added.AccountId.ShouldBe(scenario.Account.Id);
        added.Date.ShouldBe(request.Date);

        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe(request.Name);
        result.PersonId.ShouldBe(scenario.Person.Id);
        result.Type.ShouldBe(ExpenseType.Credit);
        result.Amount.ShouldBe(request.Amount);
        result.CategoryId.ShouldBe(scenario.Category.Id);
        result.AccountId.ShouldBe(scenario.Account.Id);
        result.Date.ShouldBe(request.Date);
    }

    [Fact]
    public async Task Credit_Dated_After_The_Closing_Day_Rolls_To_The_Next_Month()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 21);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 9, 1));
        scenario.WriteRepository.Added!.CompetenceMonth.ShouldBe(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public async Task Credit_Dated_On_The_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 20);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Credit_Dated_Before_The_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 5);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Theory]
    [InlineData(ExpenseType.Debit)]
    [InlineData(ExpenseType.Pix)]
    public async Task Debit_And_Pix_Ignore_The_Closing_Day(ExpenseType type)
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.Type = type;
        request.Date = new DateOnly(2026, 8, 21);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Credit_On_An_Account_With_No_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var scenario = Scenario.Build(closingDay: null);
        var request = scenario.Request();
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 21);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Explicit_Competence_Month_Overrides_The_Derived_One_And_Is_Normalised()
    {
        var scenario = Scenario.Build(closingDay: 20);
        var request = scenario.Request();
        request.Type = ExpenseType.Credit;
        // Would derive 2026-09-01 without the override.
        request.Date = new DateOnly(2026, 8, 21);
        request.CompetenceMonth = new DateOnly(2026, 11, 17);

        var result = await scenario.UseCase().Execute(request);

        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 11, 1));
        scenario.WriteRepository.Added!.CompetenceMonth.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Fact]
    public async Task Success_Pix_Without_Account()
    {
        // Arrange: same setup as the existing success test, but the account repository
        // must not be consulted, so GetById is not configured for it.
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var category = CategoryBuilder.Build(user);

        var writeRepository = new ExpenseWriteOnlyRepositoryBuilder();

        var useCase = new RegisterExpenseUseCase(
            writeRepository.Build(),
            new PersonReadOnlyRepositoryBuilder().GetById(user, person).Build(),
            new CategoryReadOnlyRepositoryBuilder().GetById(user, category).Build(),
            new AccountReadOnlyRepositoryBuilder().Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));

        var request = RequestRegisterExpenseJsonBuilder.Build(person.Id, category.Id, Guid.NewGuid());
        request.Type = ExpenseType.Pix;
        request.AccountId = null;
        request.Date = new DateOnly(2026, 8, 21);

        // Act
        var result = await useCase.Execute(request);

        // Assert
        result.AccountId.ShouldBeNull();
        // Pix never rolls to the next month, regardless of the day.
        result.CompetenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Accepts_An_Account_Of_Another_Person_Of_The_Same_User()
    {
        var user = UserBuilder.Build();
        var spender = PersonBuilder.Build(user);
        var cardHolder = PersonBuilder.Build(user);

        var category = CategoryBuilder.Build(user);
        var account = AccountBuilder.Build(cardHolder);

        var writeRepository = new ExpenseWriteOnlyRepositoryBuilder();

        var useCase = new RegisterExpenseUseCase(
            writeRepository.Build(),
            new PersonReadOnlyRepositoryBuilder().GetById(user, spender).Build(),
            new CategoryReadOnlyRepositoryBuilder().GetById(user, category).Build(),
            new AccountReadOnlyRepositoryBuilder().GetById(user, account).Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));

        var request = RequestRegisterExpenseJsonBuilder.Build(spender.Id, category.Id, account.Id);

        var result = await useCase.Execute(request);

        result.PersonId.ShouldBe(spender.Id);
        result.AccountId.ShouldBe(account.Id);
        account.PersonId.ShouldNotBe(spender.Id);

        var added = writeRepository.Added.ShouldNotBeNull();
        added.PersonId.ShouldBe(spender.Id);
        added.AccountId.ShouldBe(account.Id);
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

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Category Category { get; init; }
        public required Account Account { get; init; }
        public ExpenseWriteOnlyRepositoryBuilder WriteRepository { get; } = new();

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

        public Balance.Communication.Requests.RequestRegisterExpenseJson Request() =>
            RequestRegisterExpenseJsonBuilder.Build(Person.Id, Category.Id, Account.Id);

        public RegisterExpenseUseCase UseCase() =>
            new(
                WriteRepository.Build(),
                new PersonReadOnlyRepositoryBuilder().GetById(User, Person).Build(),
                new CategoryReadOnlyRepositoryBuilder().GetById(User, Category).Build(),
                new AccountReadOnlyRepositoryBuilder().GetById(User, Account).Build(),
                UnitOfWorkBuilder.Build(),
                LoggedUserBuilder.Build(User));
    }
}
