using System.Globalization;
using Balance.Application.UseCases.Debts.RegisterPayment;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using CommonTestUtilities.Requests;
using Shouldly;

namespace UseCases.Test.Debts.RegisterPayment;

public class RegisterDebtPaymentUseCaseTest
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
    public async Task Scheduled_Payment_Takes_Its_Reference_Month_From_The_Installment()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installmentFeb = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
        var installmentMar = DebtInstallmentBuilder.Build(debt, number: 2, referenceMonth: new DateOnly(2026, 3, 1));
        debt.Installments = [installmentFeb, installmentMar];

        scenario.DebtRepository.GetById(scenario.User, debt);

        // Paid in March, for February's installment - the payment still belongs to February.
        var request = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, installmentFeb.Id, paymentDate: new DateOnly(2026, 3, 15));

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.PaymentRepository.Added.ShouldNotBeNull();
        added.DebtInstallmentId.ShouldBe(installmentFeb.Id);
        added.ReferenceMonth.ShouldBe(new DateOnly(2026, 2, 1));

        result.DebtInstallmentId.ShouldBe(installmentFeb.Id);
        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 2, 1));

        scenario.UnitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task OpenEnded_Payment_Leaves_DebtInstallmentId_Null_And_Derives_The_Month_From_The_Payment_Date()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        scenario.DebtRepository.GetById(scenario.User, debt);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, paymentDate: new DateOnly(2026, 8, 23));

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.PaymentRepository.Added.ShouldNotBeNull();
        added.DebtInstallmentId.ShouldBeNull();
        added.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));

        result.DebtInstallmentId.ShouldBeNull();
        result.ReferenceMonth.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public async Task Success_Accepts_A_Null_Type_And_A_Null_Account()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        scenario.DebtRepository.GetById(scenario.User, debt);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id);

        var result = await scenario.UseCase().Execute(request);

        var added = scenario.PaymentRepository.Added.ShouldNotBeNull();
        added.Type.ShouldBeNull();
        added.AccountId.ShouldBeNull();

        result.Type.ShouldBeNull();
        result.AccountId.ShouldBeNull();
    }

    [Fact]
    public async Task Success_An_Account_Belonging_To_A_Different_Person_Of_The_Same_User_Is_Accepted()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        scenario.DebtRepository.GetById(scenario.User, debt);

        var otherPerson = PersonBuilder.Build(scenario.User);
        var otherPersonsAccount = AccountBuilder.Build(otherPerson);
        scenario.AccountRepository.GetById(scenario.User, otherPersonsAccount);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id, accountId: otherPersonsAccount.Id);

        var result = await scenario.UseCase().Execute(request);

        scenario.PaymentRepository.Added!.AccountId.ShouldBe(otherPersonsAccount.Id);
        result.AccountId.ShouldBe(otherPersonsAccount.Id);
    }

    [Fact]
    public async Task Error_A_Second_Payment_On_The_Same_Installment()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var debt = scenario.ScheduledDebt();
            var installment = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            debt.Installments = [installment];

            scenario.DebtRepository.GetById(scenario.User, debt);
            scenario.PaymentRepository.GetByInstallment(
                scenario.User, installment.Id, DebtPaymentBuilder.Build(debt, debtInstallmentId: installment.Id));

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id, installment.Id);

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem()
                .ShouldBe("A payment has already been recorded for this reference month.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Error_An_Archived_Debt()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var debt = scenario.OpenEndedDebt(archived: true);
            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id);

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem().ShouldBe("This debt is archived.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Error_An_Installment_Of_Another_Debt()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var debt = scenario.ScheduledDebt();
            var ownInstallment = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            debt.Installments = [ownInstallment];

            var otherDebt = scenario.ScheduledDebt();
            var foreignInstallment = DebtInstallmentBuilder.Build(otherDebt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            otherDebt.Installments = [foreignInstallment];

            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id, foreignInstallment.Id);

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Debt installment not found.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Error_A_Foreign_Account()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var debt = scenario.OpenEndedDebt();
            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id, accountId: Guid.NewGuid());

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Account not found.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    /// <summary>
    /// FINDING 4: RegisterDebtPaymentUseCase used to branch only on whether the request supplied an
    /// installment id, never on debt.Mode - so a Scheduled debt accepted an installment-less payment
    /// that moved the balance but produced no monthly line at all (GetMonthlyDebtUseCase.BuildLines
    /// iterates only debt.Installments for a Scheduled debt). The spec's Out of Scope table forbids
    /// partial payment of an installment; this was an unspecified back door to it.
    /// </summary>
    [Fact]
    public async Task Error_A_Scheduled_Debt_Payment_With_No_Installment_Id()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var debt = scenario.ScheduledDebt();
            var installment = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            debt.Installments = [installment];

            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id);

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem()
                .ShouldBe("A scheduled debt payment requires an installment id.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    /// <summary>FINDING 4: the reverse direction - an OpenEnded debt has no installment to settle.</summary>
    [Fact]
    public async Task Error_An_OpenEnded_Debt_Payment_With_An_Installment_Id()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var scheduledDebt = scenario.ScheduledDebt();
            var installment = DebtInstallmentBuilder.Build(scheduledDebt, number: 1, referenceMonth: new DateOnly(2026, 2, 1));
            scheduledDebt.Installments = [installment];

            var debt = scenario.OpenEndedDebt();
            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(debt.Id, installment.Id);

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<ErrorOnValidationException>();

            exception.GetErrors().ShouldHaveSingleItem()
                .ShouldBe("An open-ended debt payment must not have an installment id.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Error_A_Debt_Not_Owned_By_The_Logged_User()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();
            var debt = scenario.ScheduledDebt();
            scenario.DebtRepository.GetById(scenario.User, debt);

            var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());

            var act = async () => await scenario.UseCase().Execute(request);

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Debt not found.");

            scenario.PaymentRepository.Added.ShouldBeNull();
            scenario.UnitOfWork.Commits.ShouldBe(0);
        });
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

        public DebtReadOnlyRepositoryBuilder DebtRepository { get; } = new();
        public DebtPaymentRepositoryBuilder PaymentRepository { get; } = new();
        public AccountReadOnlyRepositoryBuilder AccountRepository { get; } = new();
        public UnitOfWorkBuilder UnitOfWork { get; } = new();

        public static Scenario Build()
        {
            var user = UserBuilder.Build();

            return new Scenario
            {
                User = user,
                Person = PersonBuilder.Build(user),
                Creditor = CreditorBuilder.Build(user),
                Category = CategoryBuilder.Build(user)
            };
        }

        public Debt ScheduledDebt(bool archived = false) =>
            DebtBuilder.Build(Person, Creditor, Category, DebtMode.Scheduled, archived);

        public Debt OpenEndedDebt(bool archived = false) =>
            DebtBuilder.Build(Person, Creditor, Category, DebtMode.OpenEnded, archived);

        public RegisterDebtPaymentUseCase UseCase() =>
            new(
                DebtRepository.Build(),
                PaymentRepository.Build(),
                AccountRepository.Build(),
                UnitOfWork.BuildCounting(),
                LoggedUserBuilder.Build(User));
    }
}
