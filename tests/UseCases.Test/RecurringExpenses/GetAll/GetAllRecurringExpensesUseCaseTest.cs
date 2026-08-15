using Balance.Application.UseCases.RecurringExpenses.GetAll;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace UseCases.Test.RecurringExpenses.GetAll;

public class GetAllRecurringExpensesUseCaseTest
{
    [Fact]
    public async Task Success_Returns_Every_Recurring_Expense_Including_Archived_Ones()
    {
        var loggedUser = UserBuilder.Build();
        var person = PersonBuilder.Build(loggedUser);

        var active = RecurringExpenseBuilder.Build(person, name: "Luz", archived: false);
        var archived = RecurringExpenseBuilder.Build(person, name: "Academia", archived: true);

        var readRepository = new RecurringExpenseReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [active, archived])
            .Build();

        var useCase = new GetAllRecurringExpensesUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.RecurringExpenses.Count.ShouldBe(2);

        // GetForMonth deliberately excludes archived rows; GetAll is the one surface that does not,
        // which is what makes an already-archived bill's id reachable to unarchive.
        var archivedLine = result.RecurringExpenses.Single(expense => expense.Name == "Academia");
        archivedLine.Archived.ShouldBeTrue();
        archivedLine.Id.ShouldBe(archived.Id);

        var activeLine = result.RecurringExpenses.Single(expense => expense.Name == "Luz");
        activeLine.Archived.ShouldBeFalse();
    }

    [Fact]
    public async Task Success_Carries_The_Due_Day_Estimate_Flag_And_Version_History()
    {
        var loggedUser = UserBuilder.Build();
        var person = PersonBuilder.Build(loggedUser);

        var expense = RecurringExpenseBuilder.Build(
            person, amount: 150m, dueDay: 12, isEstimate: true, type: DomainExpenseType.Pix);

        var readRepository = new RecurringExpenseReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [expense])
            .Build();

        var useCase = new GetAllRecurringExpensesUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        var line = result.RecurringExpenses.ShouldHaveSingleItem();

        line.Type.ShouldBe(CommunicationExpenseType.Pix);
        line.DueDay.ShouldBe(12);
        line.IsEstimate.ShouldBeTrue();
        line.PersonId.ShouldBe(person.Id);
        line.Versions.ShouldHaveSingleItem().Amount.ShouldBe(150m);
    }

    [Fact]
    public async Task Success_No_Recurring_Expenses_Returns_Empty_List()
    {
        var loggedUser = UserBuilder.Build();

        var readRepository = new RecurringExpenseReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [])
            .Build();

        var useCase = new GetAllRecurringExpensesUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.RecurringExpenses.ShouldBeEmpty();
    }
}
