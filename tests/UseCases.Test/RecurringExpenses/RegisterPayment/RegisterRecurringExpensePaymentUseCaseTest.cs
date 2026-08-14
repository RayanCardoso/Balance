using Balance.Application.UseCases.RecurringExpenses.RegisterPayment;
using Balance.Communication.Requests;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.RecurringExpenses.RegisterPayment;

public class RegisterRecurringExpensePaymentUseCaseTest
{
    [Fact]
    public async Task Success_Persists_The_Month_Date_Amount_Notes_And_Paying_Account()
    {
        var payingAccount = Guid.NewGuid();
        var scenario = Scenario.Build();

        var request = scenario.Request(new DateOnly(2026, 8, 1), payingAccount);
        request.AmountPaid = 180.00m;
        request.PaymentDate = new DateOnly(2026, 8, 12);
        request.Notes = "bill arrived higher";

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.PaymentRepository.Added.ShouldNotBeNull();

        added.RecurringExpenseId.ShouldBe(scenario.RecurringExpense.Id);
        added.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
        added.PaymentDate.ShouldBe(new DateOnly(2026, 8, 12));
        added.AmountPaid.ShouldBe(180.00m);
        added.Notes.ShouldBe("bill arrived higher");
        added.AccountId.ShouldBe(payingAccount);

        result.Id.ShouldBe(added.Id);
        result.RecurringExpenseId.ShouldBe(scenario.RecurringExpense.Id);
        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
        result.PaymentDate.ShouldBe(new DateOnly(2026, 8, 12));
        result.AmountPaid.ShouldBe(180.00m);
        result.Notes.ShouldBe("bill arrived higher");
        result.AccountId.ShouldBe(payingAccount);

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task The_Frozen_Version_Is_The_One_In_Effect_At_The_Reference_Month()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 1, 1));
        var onlyVersion = scenario.RecurringExpense.Versions[0];

        var result = await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 8, 1)));

        scenario.PaymentRepository.Added!.RecurringExpenseVersionId.ShouldBe(onlyVersion.Id);
        result.RecurringExpenseVersionId.ShouldBe(onlyVersion.Id);
    }

    [Fact]
    public async Task A_Payment_For_A_Month_Before_A_Value_Change_Freezes_The_Old_Version()
    {
        var scenario = Scenario.Build(amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));

        var oldVersion = scenario.RecurringExpense.Versions[0];
        oldVersion.ValidityEnd = new DateOnly(2026, 8, 31);

        var newVersion = new RecurringExpenseVersion
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = scenario.RecurringExpense.Id,
            Amount = 180.00m,
            ValidityStart = new DateOnly(2026, 9, 1),
            ValidityEnd = null,
            ChangeReason = "tariff increase"
        };

        scenario.RecurringExpense.Versions.Add(newVersion);

        var result = await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 8, 1)));

        result.RecurringExpenseVersionId.ShouldBe(oldVersion.Id);
        result.RecurringExpenseVersionId.ShouldNotBe(newVersion.Id);
        scenario.PaymentRepository.Added!.RecurringExpenseVersionId.ShouldBe(oldVersion.Id);
    }

    [Fact]
    public async Task The_Reference_Month_Is_Normalised_To_The_First_Day()
    {
        var scenario = Scenario.Build();

        var result = await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 8, 23)));

        scenario.PaymentRepository.Added!.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Success_Accepts_Null_Notes_And_A_Null_Paying_Account()
    {
        var scenario = Scenario.Build();

        var request = scenario.Request(new DateOnly(2026, 8, 1));
        request.Notes = null;
        request.AccountId = null;

        var result = await scenario.UseCase().Execute(request);

        scenario.PaymentRepository.Added!.Notes.ShouldBeNull();
        scenario.PaymentRepository.Added!.AccountId.ShouldBeNull();

        result.Notes.ShouldBeNull();
        result.AccountId.ShouldBeNull();

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Error_A_Payment_Already_Exists_For_That_Month()
    {
        var scenario = Scenario.Build();
        var referenceMonth = new DateOnly(2026, 8, 1);

        scenario.PaymentRepository.GetByMonth(
            scenario.RecurringExpense.Id,
            referenceMonth,
            new RecurringExpensePayment
            {
                RecurringExpenseId = scenario.RecurringExpense.Id,
                RecurringExpenseVersionId = scenario.RecurringExpense.Versions[0].Id,
                ReferenceMonth = referenceMonth,
                PaymentDate = new DateOnly(2026, 8, 5),
                AmountPaid = 150.00m
            });

        var act = async () => await scenario.UseCase().Execute(scenario.Request(referenceMonth));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.PAYMENT_ALREADY_RECORDED);

        scenario.PaymentRepository.Added.ShouldBeNull();
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Error_The_Recurring_Expense_Is_Archived()
    {
        var scenario = Scenario.Build(archived: true);

        var act = async () => await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 8, 1)));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.RECURRING_EXPENSE_ARCHIVED);

        scenario.PaymentRepository.Added.ShouldBeNull();
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Error_No_Version_In_Effect_At_The_Reference_Month()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 5, 1));

        var act = async () => await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 2, 1)));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.NO_VERSION_IN_EFFECT);

        scenario.PaymentRepository.Added.ShouldBeNull();
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Amount_Not_Greater_Than_Zero(decimal amountPaid)
    {
        var scenario = Scenario.Build();

        var request = scenario.Request(new DateOnly(2026, 8, 1));
        request.AmountPaid = amountPaid;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        scenario.PaymentRepository.Added.ShouldBeNull();
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Error_Recurring_Expense_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var foreignExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(otherUser));

        var loggedUser = UserBuilder.Build();

        var paymentRepository = new RecurringExpensePaymentRepositoryBuilder();
        var unitOfWork = new UnitOfWorkBuilder();

        var useCase = new RegisterRecurringExpensePaymentUseCase(
            new RecurringExpenseReadOnlyRepositoryBuilder().GetById(otherUser, foreignExpense).Build(),
            paymentRepository.Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(
            RequestRegisterRecurringExpensePaymentJsonBuilder.Build(foreignExpense.Id));

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        paymentRepository.Added.ShouldBeNull();
        unitOfWork.Commits.ShouldBe(0);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required RecurringExpense RecurringExpense { get; init; }

        public RecurringExpensePaymentRepositoryBuilder PaymentRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build(
            decimal amount = 150m, DateOnly? validityStart = null, bool archived = false)
        {
            var user = UserBuilder.Build();

            return new Scenario
            {
                User = user,
                RecurringExpense = RecurringExpenseBuilder.Build(
                    PersonBuilder.Build(user),
                    amount: amount,
                    validityStart: validityStart,
                    archived: archived)
            };
        }

        public RequestRegisterRecurringExpensePaymentJson Request(
            DateOnly? referenceMonth = null, Guid? accountId = null) =>
            RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
                RecurringExpense.Id, referenceMonth, accountId);

        public RegisterRecurringExpensePaymentUseCase UseCase() =>
            new(
                new RecurringExpenseReadOnlyRepositoryBuilder().GetById(User, RecurringExpense).Build(),
                PaymentRepository.Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
