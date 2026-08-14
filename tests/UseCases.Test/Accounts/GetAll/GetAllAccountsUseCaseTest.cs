using Balance.Application.UseCases.Accounts.GetAll;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Accounts.GetAll;

public class GetAllAccountsUseCaseTest
{
    [Fact]
    public async Task Success_Returns_Only_The_Logged_Users_Accounts()
    {
        var loggedUser = UserBuilder.Build();
        var person = PersonBuilder.Build(loggedUser);
        var account = AccountBuilder.Build(person);

        var readRepository = new AccountReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [account])
            .Build();

        var useCase = new GetAllAccountsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.Accounts.ShouldHaveSingleItem();
        result.Accounts[0].Id.ShouldBe(account.Id);
        result.Accounts[0].Name.ShouldBe(account.Name);
        result.Accounts[0].PersonId.ShouldBe(person.Id);
        result.Accounts[0].ClosingDay.ShouldBe(account.ClosingDay);
        result.Accounts[0].DueDay.ShouldBe(account.DueDay);
        result.Accounts[0].Limit.ShouldBe(account.Limit);
    }

    [Fact]
    public async Task Success_No_Accounts_Returns_Empty_List()
    {
        var loggedUser = UserBuilder.Build();

        var readRepository = new AccountReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [])
            .Build();

        var useCase = new GetAllAccountsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.Accounts.ShouldBeEmpty();
    }
}
