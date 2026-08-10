using Balance.Application.UseCases.Incomes.RegisterPayment;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Incomes.RegisterPayment;

public class RegisterIncomePaymentUseCaseTest
{
    [Fact]
    public async Task Success_Recurring_Freezes_The_Version_In_Effect()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Recurring(person, validityStart: new DateOnly(2026, 1, 1));
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(source.Id, new DateOnly(2026, 3, 1));

        var writeRepository = new IncomePaymentWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, source, writeRepository);

        var result = await useCase.Execute(request);

        result.IncomeSourceVersionId.ShouldBe(source.Versions[0].Id);
        result.AmountReceived.ShouldBe(request.AmountReceived);
        writeRepository.Added!.IncomeSourceVersionId.ShouldBe(source.Versions[0].Id);
    }

    [Fact]
    public async Task Success_Variable_Stores_No_Version()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Variable(person);
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(source.Id);

        var writeRepository = new IncomePaymentWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, source, writeRepository);

        var result = await useCase.Execute(request);

        result.IncomeSourceVersionId.ShouldBeNull();
        writeRepository.Added!.IncomeSourceVersionId.ShouldBeNull();
    }

    [Fact]
    public async Task Success_Reference_Month_Is_Normalised_To_Day_One()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Recurring(person);

        // Paid on 3 September, referring to August.
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(
            source.Id,
            referenceMonth: new DateOnly(2026, 8, 20),
            paymentDate: new DateOnly(2026, 9, 3));

        var writeRepository = new IncomePaymentWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(user, source, writeRepository);

        var result = await useCase.Execute(request);

        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
        result.PaymentDate.ShouldBe(new DateOnly(2026, 9, 3));
    }

    [Fact]
    public async Task Error_Source_Of_Another_User()
    {
        var otherUser = UserBuilder.Build();
        var otherUsersSource = IncomeSourceBuilder.Recurring(PersonBuilder.Build(otherUser));

        var loggedUser = UserBuilder.Build();
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(otherUsersSource.Id);

        var readRepository = new IncomeSourceReadOnlyRepositoryBuilder()
            .GetById(otherUser, otherUsersSource)
            .Build();

        var useCase = new RegisterIncomePaymentUseCase(
            readRepository,
            new IncomePaymentWriteOnlyRepositoryBuilder().Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(loggedUser));

        var act = async () => await useCase.Execute(request);

        var exception = await act.ShouldThrowAsync<NotFoundException>();

        exception.GetErrors().ShouldContain(ResourceErrorMessages.INCOME_SOURCE_NOT_FOUND);
    }

    [Fact]
    public async Task Error_Source_Archived()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Recurring(person);
        source.Archived = true;

        var request = RequestRegisterIncomePaymentJsonBuilder.Build(source.Id);

        var act = async () => await CreateUseCase(user, source, new IncomePaymentWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.INCOME_SOURCE_ARCHIVED);
    }

    [Fact]
    public async Task Error_No_Version_In_Effect_For_The_Reference_Month()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Recurring(person, validityStart: new DateOnly(2026, 6, 1));

        // The month predates every version of the source.
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(source.Id, new DateOnly(2026, 1, 1));

        var act = async () => await CreateUseCase(user, source, new IncomePaymentWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.NO_VERSION_IN_EFFECT);
    }

    [Fact]
    public async Task Error_Amount_Not_Greater_Than_Zero()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var source = IncomeSourceBuilder.Recurring(person);
        var request = RequestRegisterIncomePaymentJsonBuilder.Build(source.Id);
        request.AmountReceived = 0;

        var act = async () => await CreateUseCase(user, source, new IncomePaymentWriteOnlyRepositoryBuilder())
            .Execute(request);

        var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

        exception.GetErrors().ShouldHaveSingleItem().ShouldBe(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO);
    }

    private static RegisterIncomePaymentUseCase CreateUseCase(
        User user,
        IncomeSource source,
        IncomePaymentWriteOnlyRepositoryBuilder writeRepository)
    {
        var readRepository = new IncomeSourceReadOnlyRepositoryBuilder().GetById(user, source).Build();

        return new RegisterIncomePaymentUseCase(
            readRepository,
            writeRepository.Build(),
            UnitOfWorkBuilder.Build(),
            LoggedUserBuilder.Build(user));
    }
}
