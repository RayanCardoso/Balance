using Balance.Application.UseCases.People.GetAll;
using Balance.Domain.Entities;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.People.GetAll;

public class GetAllPeopleUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var loggedUser = UserBuilder.Build();
        var owner = PersonBuilder.Build(loggedUser, isAccountOwner: true);
        var spouse = PersonBuilder.Build(loggedUser);

        var useCase = CreateUseCase(loggedUser, [owner, spouse]);

        var result = await useCase.Execute();

        result.People.Count.ShouldBe(2);
        result.People.Select(person => person.Id).ShouldBe([owner.Id, spouse.Id], ignoreOrder: true);
        result.People.ShouldContain(person => person.Name == owner.Name && person.IsAccountOwner);
    }

    [Fact]
    public async Task Success_Empty_When_The_Account_Has_No_People()
    {
        var loggedUser = UserBuilder.Build();

        var useCase = CreateUseCase(loggedUser, []);

        var result = await useCase.Execute();

        result.People.ShouldBeEmpty();
    }

    [Fact]
    public async Task Another_Users_People_Are_Not_Returned()
    {
        var otherUser = UserBuilder.Build();
        var otherUsersPerson = PersonBuilder.Build(otherUser);

        var loggedUser = UserBuilder.Build();

        var repository = new PersonReadOnlyRepositoryBuilder()
            .GetAll(otherUser, [otherUsersPerson])
            .Build();

        var useCase = new GetAllPeopleUseCase(repository, MapperBuilder.Build(), LoggedUserBuilder.Build(loggedUser));

        var result = await useCase.Execute();

        result.People.ShouldBeEmpty();
    }

    private static GetAllPeopleUseCase CreateUseCase(User user, List<Person> people)
    {
        var repository = new PersonReadOnlyRepositoryBuilder().GetAll(user, people).Build();

        return new GetAllPeopleUseCase(repository, MapperBuilder.Build(), LoggedUserBuilder.Build(user));
    }
}
