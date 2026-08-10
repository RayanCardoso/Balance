using Balance.Application.UseCases.Incomes.ChangeValue;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Incomes.ChangeValue;

public class ChangeIncomeSourceValueUseCaseTest
{
    [Fact]
    public async Task Success_Closes_The_Old_Version_The_Day_Before()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), validityStart: new DateOnly(2026, 1, 1));
        var oldVersion = source.Versions[0];

        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(source.Id, new DateOnly(2026, 7, 1));

        var writeRepository = new IncomeSourceWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, source, writeRepository);

        await useCase.Execute(request);

        oldVersion.ValidityEnd.ShouldBe(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public async Task Success_Opens_A_New_Version_With_The_Change_Reason()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), validityStart: new DateOnly(2026, 1, 1));

        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(source.Id, new DateOnly(2026, 7, 1));

        var writeRepository = new IncomeSourceWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, source, writeRepository);

        var result = await useCase.Execute(request);

        result.Amount.ShouldBe(request.Amount);
        result.ExpectedDay.ShouldBe(request.ExpectedDay);
        result.ValidityStart.ShouldBe(new DateOnly(2026, 7, 1));
        result.ValidityEnd.ShouldBeNull();
        result.ChangeReason.ShouldBe(request.ChangeReason);

        writeRepository.AddedVersions.ShouldHaveSingleItem().ChangeReason.ShouldBe(request.ChangeReason);
    }

    [Fact]
    public async Task Error_Variable_Source()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Variable(PersonBuilder.Build(user));

        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(source.Id);

        var act = async () => await CreateUseCase(user, source, new IncomeSourceWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.VARIABLE_SOURCE_HAS_NO_VERSION);
    }

    [Fact]
    public async Task Error_Validity_Start_Not_Later_Than_The_Current_Version()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user), validityStart: new DateOnly(2026, 5, 1));

        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(source.Id, new DateOnly(2026, 5, 1));

        var act = async () => await CreateUseCase(user, source, new IncomeSourceWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER);
    }

    [Fact]
    public async Task Error_Change_Reason_Empty()
    {
        var user = UserBuilder.Build();
        var source = IncomeSourceBuilder.Recurring(PersonBuilder.Build(user));

        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(source.Id);
        request.ChangeReason = string.Empty;

        var act = async () => await CreateUseCase(user, source, new IncomeSourceWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.CHANGE_REASON_REQUIRED);
    }

    [Fact]
    public async Task Error_Source_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var otherUsersSource = IncomeSourceBuilder.Recurring(PersonBuilder.Build(otherUser));

        var loggedUser = UserBuilder.Build();
        var request = RequestChangeIncomeSourceValueJsonBuilder.Build(otherUsersSource.Id);

        var updateRepository = new IncomeSourceUpdateOnlyRepositoryBuilder()
            .GetById(otherUser, otherUsersSource)
            .Build();

        var useCase = new ChangeIncomeSourceValueUseCase(
            updateRepository,
            new IncomeSourceWriteOnlyRepositoryBuilder().Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.INCOME_SOURCE_NOT_FOUND);
    }

    private static ChangeIncomeSourceValueUseCase CreateUseCase(
        User user,
        IncomeSource source,
        IncomeSourceWriteOnlyRepositoryBuilder writeRepository)
    {
        var updateRepository = new IncomeSourceUpdateOnlyRepositoryBuilder().GetById(user, source).Build();

        return new ChangeIncomeSourceValueUseCase(
            updateRepository,
            writeRepository.Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));
    }
}
