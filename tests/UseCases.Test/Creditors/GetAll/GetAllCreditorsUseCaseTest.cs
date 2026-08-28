using Balance.Application.UseCases.Creditors.GetAll;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Creditors.GetAll;

public class GetAllCreditorsUseCaseTest
{
    [Fact]
    public async Task Success_Returns_Only_The_Logged_Users_Creditors()
    {
        var loggedUser = UserBuilder.Build();
        var creditor = CreditorBuilder.Build(loggedUser);

        var readRepository = new CreditorReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, false, [creditor])
            .Build();

        var useCase = new GetAllCreditorsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute(includeArchived: false);

        result.Creditors.ShouldHaveSingleItem();
        result.Creditors[0].Id.ShouldBe(creditor.Id);
        result.Creditors[0].Name.ShouldBe(creditor.Name);
    }

    [Fact]
    public async Task Success_Excludes_An_Archived_Creditor_By_Default()
    {
        var loggedUser = UserBuilder.Build();
        var active = CreditorBuilder.Build(loggedUser, archived: false);

        var readRepository = new CreditorReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, false, [active])
            .Build();

        var useCase = new GetAllCreditorsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute(includeArchived: false);

        result.Creditors.ShouldHaveSingleItem().Id.ShouldBe(active.Id);
    }

    [Fact]
    public async Task Success_Includes_An_Archived_Creditor_When_The_Flag_Is_Set()
    {
        var loggedUser = UserBuilder.Build();
        var active = CreditorBuilder.Build(loggedUser, archived: false);
        var archived = CreditorBuilder.Build(loggedUser, archived: true);

        var readRepository = new CreditorReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, true, [active, archived])
            .Build();

        var useCase = new GetAllCreditorsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute(includeArchived: true);

        result.Creditors.Count.ShouldBe(2);
        result.Creditors.ShouldContain(c => c.Id == archived.Id && c.Archived);
    }

    [Fact]
    public async Task Success_No_Creditors_Returns_Empty_List()
    {
        var loggedUser = UserBuilder.Build();

        var readRepository = new CreditorReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, false, [])
            .Build();

        var useCase = new GetAllCreditorsUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute(includeArchived: false);

        result.Creditors.ShouldBeEmpty();
    }
}
