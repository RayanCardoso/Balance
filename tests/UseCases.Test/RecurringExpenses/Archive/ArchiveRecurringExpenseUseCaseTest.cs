using Balance.Application.UseCases.RecurringExpenses.Archive;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.RecurringExpenses.Archive;

public class ArchiveRecurringExpenseUseCaseTest
{
    [Fact]
    public async Task Archiving_Sets_The_Flag()
    {
        var user = UserBuilder.Build();
        var recurringExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(user), archived: false);

        var unitOfWork = new UnitOfWorkBuilder();

        await UseCase(user, recurringExpense, unitOfWork).Execute(recurringExpense.Id, archived: true);

        recurringExpense.Archived.ShouldBeTrue();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Unarchiving_Clears_The_Flag()
    {
        var user = UserBuilder.Build();
        var recurringExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(user), archived: true);

        var unitOfWork = new UnitOfWorkBuilder();

        await UseCase(user, recurringExpense, unitOfWork).Execute(recurringExpense.Id, archived: false);

        recurringExpense.Archived.ShouldBeFalse();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Archiving_Keeps_The_Recorded_Payments()
    {
        var user = UserBuilder.Build();
        var recurringExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(user), archived: false);

        recurringExpense.Payments.Add(new RecurringExpensePayment
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = recurringExpense.Id,
            RecurringExpenseVersionId = recurringExpense.Versions[0].Id,
            ReferenceMonth = new DateOnly(2026, 8, 1),
            PaymentDate = new DateOnly(2026, 8, 10),
            AmountPaid = 180.00m
        });

        await UseCase(user, recurringExpense, new UnitOfWorkBuilder()).Execute(recurringExpense.Id, archived: true);

        recurringExpense.Archived.ShouldBeTrue();
        recurringExpense.Payments.ShouldHaveSingleItem().AmountPaid.ShouldBe(180.00m);
    }

    [Fact]
    public async Task Error_Recurring_Expense_Of_Another_User_Leaves_The_Flag_Untouched()
    {
        var otherUser = UserBuilder.Build();
        var foreignExpense = RecurringExpenseBuilder.Build(PersonBuilder.Build(otherUser), archived: false);

        var loggedUser = UserBuilder.Build();
        var unitOfWork = new UnitOfWorkBuilder();

        var useCase = new ArchiveRecurringExpenseUseCase(
            new RecurringExpenseUpdateOnlyRepositoryBuilder().GetById(otherUser, foreignExpense).Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(foreignExpense.Id, archived: true);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.RECURRING_EXPENSE_NOT_FOUND);

        foreignExpense.Archived.ShouldBeFalse();
        unitOfWork.Commits.ShouldBe(0);
    }

    private static ArchiveRecurringExpenseUseCase UseCase(
        User user,
        RecurringExpense recurringExpense,
        UnitOfWorkBuilder unitOfWork) =>
        new(
            new RecurringExpenseUpdateOnlyRepositoryBuilder().GetById(user, recurringExpense).Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(user));
}
