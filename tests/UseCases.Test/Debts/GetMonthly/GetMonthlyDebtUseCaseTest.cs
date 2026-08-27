using System.Globalization;
using Balance.Application.UseCases.Debts.GetMonthly;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;
using CommunicationExpenseStatus = Balance.Communication.Enums.ExpenseStatus;

namespace UseCases.Test.Debts.GetMonthly;

public class GetMonthlyDebtUseCaseTest
{
    private static readonly DateOnly August = new(2026, 8, 1);
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Message assertions pin literal text (L-010): never read back from ResourceErrorMessages or
    // its ResourceManager. The dev machine's ambient culture is pt-BR, so it is pinned explicitly
    // here rather than relied upon implicitly.
    private static async Task WithCulture(CultureInfo culture, Func<Task> action)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(2026, 13)]
    [InlineData(0, 8)]
    public async Task An_Invalid_Year_Or_Month_Is_Rejected(int year, int month)
    {
        await WithCulture(PtBr, async () =>
        {
            var (user, _, _, _) = NewOwner();

            var useCase = BuildUseCase(user);

            var exception = await Should.ThrowAsync<ErrorOnValidationException>(
                async () => await useCase.Execute(year, month));

            exception.GetErrors().ShouldHaveSingleItem().ShouldBe("O mês de referência é inválido.");
        });
    }

    [Fact]
    public async Task A_Month_With_Nothing_Recorded_Returns_Empty_Lines_And_Zeroed_Totals()
    {
        var (user, _, _, _) = NewOwner();

        var useCase = BuildUseCase(user, debts: []);

        var result = await useCase.Execute(2026, 8);

        result.CompetenceMonth.ShouldBe(August);
        result.Lines.ShouldBeEmpty();
        result.TotalExpected.ShouldBe(0m);
        result.TotalPaid.ShouldBe(0m);
        result.TotalCommitted.ShouldBe(0m);
    }

    [Fact]
    public async Task One_Unpaid_Installment_Of_150_Reports_150_Expected_0_Paid_150_Committed()
    {
        var (user, person, creditor, category) = NewOwner();

        var debt = DebtBuilder.Build(person, creditor, category, DebtMode.Scheduled);
        var installment = DebtInstallmentBuilder.Build(debt, referenceMonth: August, expectedAmount: 150m);
        debt.Installments.Add(installment);

        var useCase = BuildUseCase(user, debts: [debt]);

        var result = await useCase.Execute(2026, 8);

        var line = result.Lines.ShouldHaveSingleItem();
        line.Status.ShouldBe(CommunicationExpenseStatus.Pending);

        result.TotalExpected.ShouldBe(150m);
        result.TotalPaid.ShouldBe(0m);
        result.TotalCommitted.ShouldBe(150m);
    }

    /// <summary>
    /// The whole point of the committed rule: once paid at a different amount, the committed total
    /// follows what was actually PAID, not the original expectation.
    /// </summary>
    [Fact]
    public async Task Once_Paid_At_140_Reports_150_Expected_140_Paid_140_Committed()
    {
        var (user, person, creditor, category) = NewOwner();

        var debt = DebtBuilder.Build(person, creditor, category, DebtMode.Scheduled);
        var installment = DebtInstallmentBuilder.Build(debt, referenceMonth: August, expectedAmount: 150m);
        debt.Installments.Add(installment);

        var payment = DebtPaymentBuilder.Build(
            debt, debtInstallmentId: installment.Id, referenceMonth: August, amountPaid: 140m);
        debt.Payments.Add(payment);

        var useCase = BuildUseCase(user, debts: [debt]);

        var result = await useCase.Execute(2026, 8);

        var line = result.Lines.ShouldHaveSingleItem();
        line.Status.ShouldBe(CommunicationExpenseStatus.Divergent);
        line.AmountPaid.ShouldBe(140m);

        result.TotalExpected.ShouldBe(150m);
        result.TotalPaid.ShouldBe(140m);
        result.TotalCommitted.ShouldBe(140m);
    }

    /// <summary>
    /// An OpenEnded payment has no expectation at all: it adds to Paid and Committed but contributes
    /// nothing to Expected.
    /// </summary>
    [Fact]
    public async Task An_OpenEnded_Payment_Of_100_Adds_To_Paid_And_Committed_But_Nothing_To_Expected()
    {
        var (user, person, creditor, category) = NewOwner();

        var debt = DebtBuilder.Build(person, creditor, category, DebtMode.OpenEnded);
        var payment = DebtPaymentBuilder.Build(debt, referenceMonth: August, amountPaid: 100m);
        debt.Payments.Add(payment);

        var useCase = BuildUseCase(user, debts: [debt]);

        var result = await useCase.Execute(2026, 8);

        var line = result.Lines.ShouldHaveSingleItem();
        line.ExpectedAmount.ShouldBeNull();
        line.AmountPaid.ShouldBe(100m);

        result.TotalExpected.ShouldBe(0m);
        result.TotalPaid.ShouldBe(100m);
        result.TotalCommitted.ShouldBe(100m);
    }

    /// <summary>
    /// FINDING 6: the spec requires that "IF an open-ended debt receives two payments in the same
    /// month THEN the system SHALL accept both and return two lines for that month" - only the
    /// single-payment case was tested before this.
    /// </summary>
    [Fact]
    public async Task An_OpenEnded_Debt_With_Two_Payments_In_The_Same_Month_Reports_Two_Lines()
    {
        var (user, person, creditor, category) = NewOwner();

        var debt = DebtBuilder.Build(person, creditor, category, DebtMode.OpenEnded);
        var firstPayment = DebtPaymentBuilder.Build(debt, referenceMonth: August, amountPaid: 100m);
        var secondPayment = DebtPaymentBuilder.Build(debt, referenceMonth: August, amountPaid: 60m);
        debt.Payments.Add(firstPayment);
        debt.Payments.Add(secondPayment);

        var useCase = BuildUseCase(user, debts: [debt]);

        var result = await useCase.Execute(2026, 8);

        result.Lines.Count.ShouldBe(2);
        result.Lines.Select(line => line.PaymentId).ShouldBe([firstPayment.Id, secondPayment.Id], ignoreOrder: true);

        result.TotalExpected.ShouldBe(0m);
        result.TotalPaid.ShouldBe(160m);
        result.TotalCommitted.ShouldBe(160m);
    }

    /// <summary>
    /// Archived debts are excluded by the repository (GetForMonth), never re-filtered here - this
    /// pins that a debt the repository does not hand back contributes no line and no total.
    /// </summary>
    [Fact]
    public async Task An_Archived_Debt_Contributes_No_Line_And_No_Total()
    {
        var (user, _, _, _) = NewOwner();

        // GetForMonth already excludes archived debts, so the repository simply hands back none.
        var useCase = BuildUseCase(user, debts: []);

        var result = await useCase.Execute(2026, 8);

        result.Lines.ShouldBeEmpty();
        result.TotalExpected.ShouldBe(0m);
        result.TotalPaid.ShouldBe(0m);
        result.TotalCommitted.ShouldBe(0m);
    }

    private static (User User, Person Person, Creditor Creditor, Category Category) NewOwner()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);
        var creditor = CreditorBuilder.Build(user);
        var category = CategoryBuilder.Build(user);

        return (user, person, creditor, category);
    }

    private static GetMonthlyDebtUseCase BuildUseCase(
        User user,
        DateOnly? competenceMonth = null,
        List<Debt>? debts = null)
    {
        var month = competenceMonth ?? August;

        var repository = new DebtReadOnlyRepositoryBuilder()
            .GetForMonth(user, month, debts ?? [])
            .Build();

        return new GetMonthlyDebtUseCase(repository, LoggedUserBuilder.Build(user));
    }
}
