using Balance.Application.UseCases.Creditors.Archive;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Creditors.Archive;

public class ArchiveCreditorUseCaseTest
{
    [Fact]
    public async Task Archiving_Sets_The_Flag_And_Commits()
    {
        var user = UserBuilder.Build();
        var creditor = CreditorBuilder.Build(user, archived: false);

        var unitOfWork = new UnitOfWorkBuilder();

        await UseCase(user, creditor, unitOfWork).Execute(creditor.Id, archived: true);

        creditor.Archived.ShouldBeTrue();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Unarchiving_Clears_The_Flag()
    {
        var user = UserBuilder.Build();
        var creditor = CreditorBuilder.Build(user, archived: true);

        var unitOfWork = new UnitOfWorkBuilder();

        await UseCase(user, creditor, unitOfWork).Execute(creditor.Id, archived: false);

        creditor.Archived.ShouldBeFalse();
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task Error_Creditor_Of_Another_User_Throws_Not_Found()
    {
        var otherUser = UserBuilder.Build();
        var foreignCreditor = CreditorBuilder.Build(otherUser, archived: false);

        var loggedUser = UserBuilder.Build();
        var unitOfWork = new UnitOfWorkBuilder();

        var useCase = new ArchiveCreditorUseCase(
            new CreditorUpdateOnlyRepositoryBuilder().GetById(otherUser, foreignCreditor).Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(foreignCreditor.Id, archived: true);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.CREDITOR_NOT_FOUND);

        foreignCreditor.Archived.ShouldBeFalse();
        unitOfWork.Commits.ShouldBe(0);
    }

    private static ArchiveCreditorUseCase UseCase(User user, Creditor creditor, UnitOfWorkBuilder unitOfWork) =>
        new(
            new CreditorUpdateOnlyRepositoryBuilder().GetById(user, creditor).Build(),
            unitOfWork.BuildCounting(),
            LoggedUserBuilder.Build(user));
}
