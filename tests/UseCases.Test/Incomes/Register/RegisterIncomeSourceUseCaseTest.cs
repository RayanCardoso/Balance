using Balance.Application.UseCases.Incomes.Register;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;
using DomainIncomeType = Balance.Domain.Enums.IncomeType;

namespace UseCases.Test.Incomes.Register;

public class RegisterIncomeSourceUseCaseTest
{
    [Fact]
    public async Task Success_Recurring_Creates_One_Open_Version()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(person.Id);

        var writeRepository = new IncomeSourceWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, person, writeRepository);

        var result = await useCase.Execute(request);

        result.Name.ShouldBe(request.Name);
        result.Amount.ShouldBe(request.Amount);
        result.ExpectedDay.ShouldBe(request.ExpectedDay);
        result.Archived.ShouldBeFalse();

        writeRepository.AddedSource!.Type.ShouldBe(DomainIncomeType.Recurring);
        writeRepository.AddedVersions.Count.ShouldBe(1);
        writeRepository.AddedVersions[0].ValidityEnd.ShouldBeNull();
        writeRepository.AddedVersions[0].ValidityStart.ShouldBe(request.ValidityStart!.Value);
        writeRepository.AddedVersions[0].Amount.ShouldBe(request.Amount!.Value);
    }

    [Fact]
    public async Task Success_Variable_Creates_No_Version()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterIncomeSourceJsonBuilder.Variable(person.Id);

        var writeRepository = new IncomeSourceWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, person, writeRepository);

        var result = await useCase.Execute(request);

        result.Amount.ShouldBeNull();
        result.ExpectedDay.ShouldBeNull();

        writeRepository.AddedSource!.Type.ShouldBe(DomainIncomeType.Variable);
        writeRepository.AddedVersions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Error_Person_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var otherUsersPerson = PersonBuilder.Build(otherUser);

        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(otherUsersPerson.Id);

        // The repository is seeded for the other user only, so the logged user finds nothing.
        var personRepository = new PersonReadOnlyRepositoryBuilder()
            .GetById(otherUser, otherUsersPerson)
            .Build();

        var useCase = new RegisterIncomeSourceUseCase(
            new IncomeSourceWriteOnlyRepositoryBuilder().Build(),
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
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(person.Id);
        request.Name = string.Empty;

        var act = async () => await CreateUseCase(user, person, new IncomeSourceWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.NAME_REQUIRED);
    }

    [Fact]
    public async Task Error_Amount_Not_Greater_Than_Zero()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var request = RequestRegisterIncomeSourceJsonBuilder.Recurring(person.Id);
        request.Amount = 0;

        var act = async () => await CreateUseCase(user, person, new IncomeSourceWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    private static RegisterIncomeSourceUseCase CreateUseCase(
        User user,
        Person person,
        IncomeSourceWriteOnlyRepositoryBuilder writeRepository)
    {
        var personRepository = new PersonReadOnlyRepositoryBuilder().GetById(user, person).Build();

        return new RegisterIncomeSourceUseCase(
            writeRepository.Build(),
            personRepository,
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));
    }
}
