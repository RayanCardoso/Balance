using Balance.Application.UseCases.RecurringExpenses.ChangeValue;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.RecurringExpenses.ChangeValue;

public class ChangeRecurringExpenseValueUseCaseTest
{
    [Fact]
    public async Task Success_Closes_The_Version_In_Effect_The_Day_Before_The_New_Start()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 1, 1));
        var oldVersion = scenario.RecurringExpense.Versions[0];

        await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 9, 1)));

        oldVersion.ValidityEnd.ShouldBe(new DateOnly(2026, 8, 31));
    }

    [Fact]
    public async Task Success_Opens_A_New_Version_Carrying_The_Amount_Start_And_Reason()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 1, 1));
        var request = scenario.Request(new DateOnly(2026, 9, 1));
        request.Amount = 180.00m;

        var result = await scenario.UseCase().Execute(request);

        var newVersion = scenario.WriteRepository.AddedVersions.ShouldHaveSingleItem();

        newVersion.RecurringExpenseId.ShouldBe(scenario.RecurringExpense.Id);
        newVersion.Amount.ShouldBe(180.00m);
        newVersion.ValidityStart.ShouldBe(new DateOnly(2026, 9, 1));
        newVersion.ValidityEnd.ShouldBeNull();
        newVersion.ChangeReason.ShouldBe(request.ChangeReason);

        result.Versions.Count.ShouldBe(2);
        result.Versions[1].Amount.ShouldBe(180.00m);
        result.Versions[1].ValidityEnd.ShouldBeNull();
        result.Versions[1].ChangeReason.ShouldBe(request.ChangeReason);
    }

    [Fact]
    public async Task The_Timeline_Has_No_Gap_And_No_Overlap()
    {
        var scenario = Scenario.Build(amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));
        var request = scenario.Request(new DateOnly(2026, 9, 1));
        request.Amount = 180.00m;

        var result = await scenario.UseCase().Execute(request);

        result.Versions[0].Amount.ShouldBe(150.00m);
        result.Versions[0].ValidityStart.ShouldBe(new DateOnly(2026, 1, 1));
        result.Versions[0].ValidityEnd.ShouldBe(new DateOnly(2026, 8, 31));

        result.Versions[1].ValidityStart.ShouldBe(new DateOnly(2026, 9, 1));
        result.Versions[0].ValidityEnd!.Value.AddDays(1).ShouldBe(result.Versions[1].ValidityStart);
    }

    [Fact]
    public async Task The_Closing_And_The_New_Version_Are_Saved_In_One_Transaction()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 1, 1));

        await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 9, 1)));

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task A_Previously_Recorded_Payment_Keeps_Its_Version_Identifier()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 1, 1));

        var payment = new RecurringExpensePayment
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = scenario.RecurringExpense.Id,
            RecurringExpenseVersionId = scenario.RecurringExpense.Versions[0].Id,
            ReferenceMonth = new DateOnly(2026, 8, 1),
            PaymentDate = new DateOnly(2026, 8, 10),
            AmountPaid = 152.40m
        };

        scenario.RecurringExpense.Payments.Add(payment);

        var frozenVersionId = payment.RecurringExpenseVersionId;

        await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 9, 1)));

        payment.RecurringExpenseVersionId.ShouldBe(frozenVersionId);
        payment.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
        payment.AmountPaid.ShouldBe(152.40m);
    }

    [Fact]
    public async Task Error_Change_Reason_Empty()
    {
        var scenario = Scenario.Build();
        var request = scenario.Request(new DateOnly(2026, 9, 1));
        request.ChangeReason = string.Empty;

        var act = async () => await scenario.UseCase().Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.CHANGE_REASON_REQUIRED);
    }

    [Fact]
    public async Task Error_Validity_Start_Equal_To_The_Version_In_Effect()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 5, 1));

        var act = async () => await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 5, 1)));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER);
    }

    [Fact]
    public async Task Error_Validity_Start_Earlier_Than_The_Version_In_Effect()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 5, 1));

        var act = async () => await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 3, 1)));

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER);
    }

    [Fact]
    public async Task A_Rejected_Change_Leaves_The_Version_In_Effect_Open()
    {
        var scenario = Scenario.Build(validityStart: new DateOnly(2026, 5, 1));
        var currentVersion = scenario.RecurringExpense.Versions[0];

        var act = async () => await scenario.UseCase().Execute(scenario.Request(new DateOnly(2026, 5, 1)));

        await act.ShouldThrowAsync<ErrorOnValidationException>();

        currentVersion.ValidityEnd.ShouldBeNull();
        scenario.WriteRepository.AddedVersions.ShouldBeEmpty();
        scenario.UnitOfWork.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task Error_Recurring_Expense_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var foreignExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(otherUser));

        var loggedUser = UserBuilder.Build();

        var writeRepository = new RecurringExpenseWriteOnlyRepositoryBuilder();

        var useCase = new ChangeRecurringExpenseValueUseCase(
            new RecurringExpenseUpdateOnlyRepositoryBuilder().GetById(otherUser, foreignExpense).Build(),
            writeRepository.Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(
            RequestChangeRecurringExpenseValueJsonBuilder.Build(foreignExpense.Id, new DateOnly(2026, 9, 1)));

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        foreignExpense.Versions[0].ValidityEnd.ShouldBeNull();
        writeRepository.AddedVersions.ShouldBeEmpty();
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required RecurringExpense RecurringExpense { get; init; }

        public RecurringExpenseWriteOnlyRepositoryBuilder WriteRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build(decimal amount = 150m, DateOnly? validityStart = null)
        {
            var user = UserBuilder.Build();

            return new Scenario
            {
                User = user,
                RecurringExpense = RecurringExpenseBuilder.Build(
                    PersonBuilder.Build(user), amount: amount, validityStart: validityStart)
            };
        }

        public Balance.Communication.Requests.RequestChangeRecurringExpenseValueJson Request(DateOnly validityStart) =>
            RequestChangeRecurringExpenseValueJsonBuilder.Build(RecurringExpense.Id, validityStart);

        public ChangeRecurringExpenseValueUseCase UseCase() =>
            new(
                new RecurringExpenseUpdateOnlyRepositoryBuilder().GetById(User, RecurringExpense).Build(),
                WriteRepository.Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
