using System.Globalization;
using Balance.Application.UseCases.Debts.UpdatePayment;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace UseCases.Test.Debts.UpdatePayment;

public class UpdateDebtPaymentUseCaseTest
{
    // Message assertions pin literal text (L-010), which requires a fixed culture rather than
    // whatever the host machine's ambient thread culture happens to be.
    private static async Task WithInvariantCulture(Func<Task> action)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task Success_Overwrites_The_Amount_Date_Type_Account_And_Notes()
    {
        var scenario = Scenario.Build();
        var payingAccount = AccountBuilder.Build(scenario.Person);
        scenario.AccountRepository.GetById(scenario.User, payingAccount);

        var request = RequestUpdateDebtPaymentJsonBuilder.Build(
            amountPaid: 172.40m,
            paymentDate: new DateOnly(2026, 3, 15),
            accountId: payingAccount.Id,
            type: CommunicationExpenseType.Pix);
        request.Notes = "corrected after the bill was re-read";

        var result = await scenario.UseCase().Execute(scenario.Payment.Id, request);

        scenario.Payment.AmountPaid.ShouldBe(172.40m);
        scenario.Payment.PaymentDate.ShouldBe(new DateOnly(2026, 3, 15));
        scenario.Payment.Type.ShouldBe(DomainExpenseType.Pix);
        scenario.Payment.AccountId.ShouldBe(payingAccount.Id);
        scenario.Payment.Notes.ShouldBe("corrected after the bill was re-read");

        result.Id.ShouldBe(scenario.Payment.Id);
        result.AmountPaid.ShouldBe(172.40m);
        result.PaymentDate.ShouldBe(new DateOnly(2026, 3, 15));
        result.Type.ShouldBe(CommunicationExpenseType.Pix);
        result.AccountId.ShouldBe(payingAccount.Id);
        result.AccountName.ShouldBe(payingAccount.Name);
        result.Notes.ShouldBe("corrected after the bill was re-read");

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    /// <summary>
    /// FINDING 2: the account id must resolve through the logged user, exactly like
    /// RegisterDebtPaymentUseCase - an id that satisfies the foreign key but belongs to someone
    /// else's account must 404, never be silently assigned.
    /// </summary>
    [Fact]
    public async Task Error_Correcting_With_An_Account_Not_Owned_By_The_Logged_User()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var request = RequestUpdateDebtPaymentJsonBuilder.Build(
                amountPaid: 172.40m, accountId: Guid.NewGuid());

            var act = async () => await scenario.UseCase().Execute(scenario.Payment.Id, request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Account not found.");

            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task The_Reference_Month_And_The_Installment_Id_Are_Unchanged_After_The_Call()
    {
        var scenario = Scenario.Build();
        var referenceMonthBefore = scenario.Payment.ReferenceMonth;
        var installmentIdBefore = scenario.Payment.DebtInstallmentId;
        var debtIdBefore = scenario.Payment.DebtId;

        var result = await scenario.UseCase().Execute(
            scenario.Payment.Id, RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: 172.40m));

        scenario.Payment.ReferenceMonth.ShouldBe(referenceMonthBefore);
        scenario.Payment.DebtInstallmentId.ShouldBe(installmentIdBefore);
        scenario.Payment.DebtId.ShouldBe(debtIdBefore);

        result.ReferenceMonth.ShouldBe(referenceMonthBefore);
        result.DebtInstallmentId.ShouldBe(installmentIdBefore);
        result.DebtId.ShouldBe(debtIdBefore);
    }

    [Fact]
    public async Task Clearing_The_Type_To_Null_Is_Persisted_As_Null()
    {
        var scenario = Scenario.Build(type: DomainExpenseType.Pix);

        var request = RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: 172.40m);
        request.Type = null;

        var result = await scenario.UseCase().Execute(scenario.Payment.Id, request);

        scenario.Payment.Type.ShouldBeNull();
        result.Type.ShouldBeNull();
    }

    [Fact]
    public async Task Error_A_Payment_Not_Owned_By_The_Logged_User()
    {
        await WithInvariantCulture(async () =>
        {
            var otherUser = UserBuilder.Build();
            var foreignPerson = PersonBuilder.Build(otherUser);
            var foreignCreditor = CreditorBuilder.Build(otherUser);
            var foreignCategory = CategoryBuilder.Build(otherUser);
            var foreignDebt = DebtBuilder.Build(foreignPerson, foreignCreditor, foreignCategory, DebtMode.OpenEnded);
            var foreignPayment = DebtPaymentBuilder.Build(foreignDebt);

            var loggedUser = UserBuilder.Build();
            var unitOfWork = new UnitOfWorkBuilder();

            var useCase = new UpdateDebtPaymentUseCase(
                new DebtPaymentRepositoryBuilder().GetById(otherUser, foreignPayment).Build(),
                new AccountReadOnlyRepositoryBuilder().Build(),
                unitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(loggedUser));

            var act = async () => await useCase.Execute(
                foreignPayment.Id, RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: 172.40m));

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Debt payment not found.");

            unitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Error_Correcting_To_Credit_With_No_Account()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var request = RequestUpdateDebtPaymentJsonBuilder.Build(
                amountPaid: 172.40m, type: CommunicationExpenseType.Credit, accountId: null);

            var act = async () => await scenario.UseCase().Execute(scenario.Payment.Id, request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem().ShouldBe("An account is required for a credit expense.");

            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Error_Amount_Not_Greater_Than_Zero(decimal amountPaid)
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var request = RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: amountPaid);

            var act = async () => await scenario.UseCase().Execute(scenario.Payment.Id, request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem().ShouldBe("The amount must be greater than zero.");

            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Debt Debt { get; init; }
        public required DebtPayment Payment { get; init; }

        public AccountReadOnlyRepositoryBuilder AccountRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build(Guid? accountId = null, DomainExpenseType? type = null)
        {
            var user = UserBuilder.Build();
            var person = PersonBuilder.Build(user);
            var creditor = CreditorBuilder.Build(user);
            var category = CategoryBuilder.Build(user);
            var debt = DebtBuilder.Build(person, creditor, category, DebtMode.Scheduled);
            var installment = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            debt.Installments = [installment];

            var payment = DebtPaymentBuilder.Build(
                debt,
                debtInstallmentId: installment.Id,
                referenceMonth: installment.ReferenceMonth,
                accountId: accountId,
                type: type);

            return new Scenario
            {
                User = user,
                Person = person,
                Debt = debt,
                Payment = payment
            };
        }

        public UpdateDebtPaymentUseCase UseCase() =>
            new(
                new DebtPaymentRepositoryBuilder().GetById(User, Payment).Build(),
                AccountRepository.Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
