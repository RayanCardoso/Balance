using Balance.Application.UseCases.Accounts.Register;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Accounts.Register;

public class RegisterAccountUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterAccountJsonBuilder.Build(person.Id);

        var writeRepository = new AccountWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, person, writeRepository);

        var result = await useCase.Execute(request);

        result.Name.ShouldBe(request.Name);
        result.Institution.ShouldBe(request.Institution);
        result.PersonId.ShouldBe(person.Id);
        result.ClosingDay.ShouldBe(request.ClosingDay);
        result.DueDay.ShouldBe(request.DueDay);
        result.Limit.ShouldBe(request.Limit);
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Success_Debit_Account_With_No_Card_Fields()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterAccountJsonBuilder.Debit(person.Id);

        var writeRepository = new AccountWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, person, writeRepository);

        var result = await useCase.Execute(request);

        result.ClosingDay.ShouldBeNull();
        result.DueDay.ShouldBeNull();
        result.Limit.ShouldBeNull();
    }

    [Fact]
    public async Task Error_Person_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var otherUsersPerson = PersonBuilder.Build(otherUser);

        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterAccountJsonBuilder.Build(otherUsersPerson.Id);

        // The repository is seeded for the other user only, so the logged user finds nothing.
        var personRepository = new PersonReadOnlyRepositoryBuilder()
            .GetById(otherUser, otherUsersPerson)
            .Build();

        var useCase = new RegisterAccountUseCase(
            new AccountWriteOnlyRepositoryBuilder().Build(),
            personRepository,
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.PERSON_NOT_FOUND);
    }

    [Fact]
    public async Task Error_Name_Empty()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterAccountJsonBuilder.Build(person.Id);
        request.Name = string.Empty;

        var act = async () => await CreateUseCase(user, person, new AccountWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.NAME_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Error_Closing_Day_Out_Of_Range(int closingDay)
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterAccountJsonBuilder.Build(person.Id);
        request.ClosingDay = closingDay;

        var act = async () => await CreateUseCase(user, person, new AccountWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Error_Due_Day_Out_Of_Range(int dueDay)
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterAccountJsonBuilder.Build(person.Id);
        request.DueDay = dueDay;

        var act = async () => await CreateUseCase(user, person, new AccountWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.DAY_OUT_OF_RANGE);
    }

    private static RegisterAccountUseCase CreateUseCase(
        User user,
        Person person,
        AccountWriteOnlyRepositoryBuilder writeRepository)
    {
        var personRepository = new PersonReadOnlyRepositoryBuilder().GetById(user, person).Build();

        return new RegisterAccountUseCase(
            writeRepository.Build(),
            personRepository,
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));
    }
}
