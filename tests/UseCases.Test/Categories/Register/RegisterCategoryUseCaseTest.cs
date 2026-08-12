using Balance.Application.UseCases.Categories.Register;
using Balance.Communication.Enums;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Categories.Register;

public class RegisterCategoryUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCategoryJsonBuilder.Build();

        var writeRepository = new CategoryWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        var result = await useCase.Execute(request);

        result.Name.ShouldBe(request.Name);
        result.Description.ShouldBe(request.Description);
        result.Priority.ShouldBe(request.Priority);
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Success_Links_The_Category_To_The_Logged_User()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCategoryJsonBuilder.Build();

        var writeRepository = new CategoryWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        await useCase.Execute(request);

        writeRepository.Added.ShouldNotBeNull();
        writeRepository.Added!.UserId.ShouldBe(loggedUser.Id);
    }

    [Fact]
    public async Task Error_Name_Empty()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterCategoryJsonBuilder.Build();
        request.Name = string.Empty;

        var useCase = CreateUseCase(loggedUser, new CategoryWriteOnlyRepositoryBuilder());

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.NAME_REQUIRED);
    }

    private static RegisterCategoryUseCase CreateUseCase(User user, CategoryWriteOnlyRepositoryBuilder writeRepository)
    {
        var unitOfWork = UnitOfWorkBuilder.Build();
        var loggedUser = LoggedUserBuilder.Build(user);

        return new RegisterCategoryUseCase(writeRepository.Build(), unitOfWork, loggedUser);
    }
}
