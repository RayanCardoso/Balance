using Balance.Application.UseCases.Categories.GetAll;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Categories.GetAll;

public class GetAllCategoriesUseCaseTest
{
    [Fact]
    public async Task Success_Returns_Only_The_Logged_Users_Categories()
    {
        var loggedUser = UserBuilder.Build();
        var category = CategoryBuilder.Build(loggedUser);

        var readRepository = new CategoryReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [category])
            .Build();

        var useCase = new GetAllCategoriesUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.Categories.ShouldHaveSingleItem();
        result.Categories[0].Id.ShouldBe(category.Id);
        result.Categories[0].Name.ShouldBe(category.Name);
        result.Categories[0].Description.ShouldBe(category.Description);
        result.Categories[0].Priority.ShouldBe((Balance.Communication.Enums.ExpensePriority)category.Priority);
    }

    [Fact]
    public async Task Success_No_Categories_Returns_Empty_List()
    {
        var loggedUser = UserBuilder.Build();

        var readRepository = new CategoryReadOnlyRepositoryBuilder()
            .GetAll(loggedUser, [])
            .Build();

        var useCase = new GetAllCategoriesUseCase(readRepository, LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.Categories.ShouldBeEmpty();
    }
}
