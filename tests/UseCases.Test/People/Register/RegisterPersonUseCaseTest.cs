using Balance.Application.UseCases.People.Register;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Mapper;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.People.Register;

public class RegisterPersonUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterPersonJsonBuilder.Build();

        var writeRepository = new PersonWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        var result = await useCase.Execute(request);

        result.Name.ShouldBe(request.Name);
        result.Description.ShouldBe(request.Description);
        result.IsAccountOwner.ShouldBeFalse();
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Success_Links_The_Person_To_The_Logged_User()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterPersonJsonBuilder.Build();

        var writeRepository = new PersonWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(loggedUser, writeRepository);

        await useCase.Execute(request);

        writeRepository.Added.ShouldNotBeNull();
        writeRepository.Added!.UserId.ShouldBe(loggedUser.Id);
        writeRepository.Added.IsAccountOwner.ShouldBeFalse();
    }

    [Fact]
    public async Task Error_Name_Empty()
    {
        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterPersonJsonBuilder.Build();
        request.Name = string.Empty;

        var useCase = CreateUseCase(loggedUser, new PersonWriteOnlyRepositoryBuilder());

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().Count.ShouldBe(1);
        exception.GetErrors().ShouldContain(ResourceErrorMessages.NAME_REQUIRED);
    }

    private static RegisterPersonUseCase CreateUseCase(User user, PersonWriteOnlyRepositoryBuilder writeRepository)
    {
        var mapper = MapperBuilder.Build();
        var unitOfWork = UnitOfWorkBuilder.Build();
        var loggedUser = LoggedUserBuilder.Build(user);

        return new RegisterPersonUseCase(writeRepository.Build(), unitOfWork, mapper, loggedUser);
    }
}
