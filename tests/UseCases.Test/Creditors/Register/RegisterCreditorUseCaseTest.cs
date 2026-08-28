using Balance.Application.UseCases.Creditors.Register;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Creditors.Register;

public class RegisterCreditorUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCreditorJsonBuilder.Build();

        var writeRepository = new CreditorWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        var result = await useCase.Execute(request);

        result.Name.ShouldBe(request.Name);
        result.Type.ShouldBe(request.Type);
        result.Contact.ShouldBe(request.Contact);
        result.Notes.ShouldBe(request.Notes);
        result.Archived.ShouldBeFalse();
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Success_Links_The_Creditor_To_The_Logged_User()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCreditorJsonBuilder.Build();

        var writeRepository = new CreditorWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        await useCase.Execute(request);

        writeRepository.Added.ShouldNotBeNull();
        writeRepository.Added!.UserId.ShouldBe(loggedUser.Id);
    }

    [Fact]
    public async Task Success_Contact_Null()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Contact = null;

        var useCase = CreateUseCase(loggedUser, new CreditorWriteOnlyRepositoryBuilder());

        var result = await useCase.Execute(request);

        result.Contact.ShouldBeNull();
    }

    [Fact]
    public async Task Success_Notes_Null()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Notes = null;

        var useCase = CreateUseCase(loggedUser, new CreditorWriteOnlyRepositoryBuilder());

        var result = await useCase.Execute(request);

        result.Notes.ShouldBeNull();
    }

    [Fact]
    public async Task Success_Two_Creditors_Of_The_Same_User_May_Share_A_Name()
    {
        var loggedUser = UserBuilder.Build();

        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Name = "Banco";

        var otherRequest = RequestRegisterCreditorJsonBuilder.Build();
        otherRequest.Name = "Banco";

        var useCase = CreateUseCase(loggedUser, new CreditorWriteOnlyRepositoryBuilder());

        var first = await useCase.Execute(request);
        var second = await useCase.Execute(otherRequest);

        first.Name.ShouldBe("Banco");
        second.Name.ShouldBe("Banco");
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public async Task Error_Name_Empty()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Name = string.Empty;

        var useCase = CreateUseCase(loggedUser, new CreditorWriteOnlyRepositoryBuilder());

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
    }

    private static RegisterCreditorUseCase CreateUseCase(
        User user,
        CreditorWriteOnlyRepositoryBuilder writeRepository)
    {
        var unitOfWork = UnitOfWorkBuilder.Build();
        var loggedUser = LoggedUserBuilder.Build(user);

        return new RegisterCreditorUseCase(writeRepository.Build(), unitOfWork, loggedUser);
    }
}
