using Balance.Application.UseCases.RecurringExpenses.UpdatePayment;
using Balance.Communication.Requests;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.RecurringExpenses.UpdatePayment;

public class UpdateRecurringExpensePaymentUseCaseTest
{
    [Fact]
    public async Task Success_Overwrites_The_Amount_Date_Notes_And_Paying_Account()
    {
        var payingAccount = Guid.NewGuid();
        var scenario = Scenario.Build(amountPaid: 180.00m);

        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(
            amountPaid: 172.40m, paymentDate: new DateOnly(2026, 8, 15), accountId: payingAccount);
        request.Notes = "corrected after the bill was re-read";

        var result = await scenario.UseCase().Execute(scenario.Payment.Id, request);

        scenario.Payment.AmountPaid.ShouldBe(172.40m);
        scenario.Payment.PaymentDate.ShouldBe(new DateOnly(2026, 8, 15));
        scenario.Payment.Notes.ShouldBe("corrected after the bill was re-read");
        scenario.Payment.AccountId.ShouldBe(payingAccount);

        result.Id.ShouldBe(scenario.Payment.Id);
        result.AmountPaid.ShouldBe(172.40m);
        result.PaymentDate.ShouldBe(new DateOnly(2026, 8, 15));
        result.Notes.ShouldBe("corrected after the bill was re-read");
        result.AccountId.ShouldBe(payingAccount);

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task The_Reference_Month_And_The_Frozen_Version_Are_Unchanged()
    {
        var scenario = Scenario.Build();

        var referenceMonthBefore = scenario.Payment.ReferenceMonth;
        var frozenVersionBefore = scenario.Payment.RecurringExpenseVersionId;

        var result = await scenario.UseCase().Execute(
            scenario.Payment.Id, RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m));

        scenario.Payment.ReferenceMonth.ShouldBe(referenceMonthBefore);
        scenario.Payment.RecurringExpenseVersionId.ShouldBe(frozenVersionBefore);

        result.ReferenceMonth.ShouldBe(referenceMonthBefore);
        result.RecurringExpenseVersionId.ShouldBe(frozenVersionBefore);
        result.RecurringExpenseId.ShouldBe(scenario.RecurringExpense.Id);
    }

    [Fact]
    public async Task A_Correction_Keeps_The_Frozen_Version_Even_When_The_Month_Now_Resolves_To_A_Newer_One()
    {
        var scenario = Scenario.Build();

        // The value changed from the reference month itself, so August now resolves to the new
        // version. The payment was measured against the old one and must keep it: recorded history
        // is not rewritten by a later change.
        var oldVersion = scenario.RecurringExpense.Versions[0];
        oldVersion.ValidityEnd = new DateOnly(2026, 7, 31);

        var newVersion = new RecurringExpenseVersion
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = scenario.RecurringExpense.Id,
            Amount = 180.00m,
            ValidityStart = new DateOnly(2026, 8, 1),
            ValidityEnd = null,
            ChangeReason = "tariff increase"
        };

        scenario.RecurringExpense.Versions.Add(newVersion);

        var result = await scenario.UseCase().Execute(
            scenario.Payment.Id, RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m));

        result.RecurringExpenseVersionId.ShouldBe(oldVersion.Id);
        result.RecurringExpenseVersionId.ShouldNotBe(newVersion.Id);
        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Success_Clears_The_Notes_And_The_Paying_Account_When_They_Are_Omitted()
    {
        var scenario = Scenario.Build(accountId: Guid.NewGuid());

        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m);
        request.Notes = null;
        request.AccountId = null;

        var result = await scenario.UseCase().Execute(scenario.Payment.Id, request);

        scenario.Payment.Notes.ShouldBeNull();
        scenario.Payment.AccountId.ShouldBeNull();

        result.Notes.ShouldBeNull();
        result.AccountId.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Amount_Not_Greater_Than_Zero(decimal amountPaid)
    {
        var scenario = Scenario.Build(amountPaid: 180.00m);

        var act = async () => await scenario.UseCase().Execute(
            scenario.Payment.Id, RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: amountPaid));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);

        scenario.Payment.AmountPaid.ShouldBe(180.00m);
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Error_Payment_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var foreignExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(otherUser));
        var foreignPayment = RecurringExpensePaymentBuilder.Build(foreignExpense, amountPaid: 180.00m);

        var loggedUser = UserBuilder.Build();

        var unitOfWork = new UnitOfWorkBuilder();

        var useCase = new UpdateRecurringExpensePaymentUseCase(
            new RecurringExpensePaymentRepositoryBuilder().GetById(otherUser, foreignPayment).Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(
            foreignPayment.Id, RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m));

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.RECURRING_EXPENSE_PAYMENT_NOT_FOUND);

        foreignPayment.AmountPaid.ShouldBe(180.00m);
        unitOfWork.Commits.ShouldBe(0);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required RecurringExpense RecurringExpense { get; init; }
        public required RecurringExpensePayment Payment { get; init; }

        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build(decimal amountPaid = 180.00m, Guid? accountId = null)
        {
            var user = UserBuilder.Build();
            var recurringExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(user));

            return new Scenario
            {
                User = user,
                RecurringExpense = recurringExpense,
                Payment = RecurringExpensePaymentBuilder.Build(
                    recurringExpense, amountPaid: amountPaid, accountId: accountId)
            };
        }

        public UpdateRecurringExpensePaymentUseCase UseCase() =>
            new(
                new RecurringExpensePaymentRepositoryBuilder().GetById(User, Payment).Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
