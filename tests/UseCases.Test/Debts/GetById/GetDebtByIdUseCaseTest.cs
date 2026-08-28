using System.Globalization;
using Balance.Application.UseCases.Debts.GetById;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;

namespace UseCases.Test.Debts.GetById;

public class GetDebtByIdUseCaseTest
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
    public async Task Success_Two_Payments_Report_The_Reduced_Balance_And_Are_Not_Settled()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt(totalAmount: 1500m);
        debt.Payments =
        [
            DebtPaymentBuilder.Build(debt, amountPaid: 150m, referenceMonth: new DateOnly(2026, 1, 1)),
            DebtPaymentBuilder.Build(debt, amountPaid: 150m, referenceMonth: new DateOnly(2026, 2, 1))
        ];

        scenario.DebtRepository.GetById(scenario.User, debt);

        var result = await scenario.UseCase().Execute(debt.Id);

        result.OutstandingBalance.ShouldBe(1200m);
        result.IsSettled.ShouldBeFalse();
    }

    [Fact]
    public async Task Success_A_Fully_Paid_Debt_Reports_Zero_And_Is_Settled()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt(totalAmount: 1500m);
        debt.Payments = [DebtPaymentBuilder.Build(debt, amountPaid: 1500m)];

        scenario.DebtRepository.GetById(scenario.User, debt);

        var result = await scenario.UseCase().Execute(debt.Id);

        result.OutstandingBalance.ShouldBe(0m);
        result.IsSettled.ShouldBeTrue();
    }

    [Fact]
    public async Task Success_Installments_And_Payments_Come_Back_Ordered_Despite_An_Out_Of_Order_Fixture()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();

        // Deliberately built out of order - Number 3, 1, 2 and payment dates late, early, middle -
        // to prove the use case orders them itself rather than trusting the source order.
        var installment1 = DebtInstallmentBuilder.Build(debt, number: 1, referenceMonth: new DateOnly(2026, 1, 1));
        var installment2 = DebtInstallmentBuilder.Build(debt, number: 2, referenceMonth: new DateOnly(2026, 2, 1));
        var installment3 = DebtInstallmentBuilder.Build(debt, number: 3, referenceMonth: new DateOnly(2026, 3, 1));
        debt.Installments = [installment3, installment1, installment2];

        var paymentLate = DebtPaymentBuilder.Build(debt, referenceMonth: new DateOnly(2026, 3, 1));
        var paymentEarly = DebtPaymentBuilder.Build(debt, referenceMonth: new DateOnly(2026, 1, 1));
        var paymentMiddle = DebtPaymentBuilder.Build(debt, referenceMonth: new DateOnly(2026, 2, 1));
        debt.Payments = [paymentLate, paymentEarly, paymentMiddle];

        scenario.DebtRepository.GetById(scenario.User, debt);

        var result = await scenario.UseCase().Execute(debt.Id);

        result.Installments.Select(i => i.Number).ShouldBe([1, 2, 3]);
        result.Payments.Select(p => p.Id).ShouldBe([paymentEarly.Id, paymentMiddle.Id, paymentLate.Id]);
    }

    [Fact]
    public async Task Success_The_Creditor_Name_And_Type_Are_Carried_Through()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();

        scenario.DebtRepository.GetById(scenario.User, debt);

        var result = await scenario.UseCase().Execute(debt.Id);

        result.CreditorId.ShouldBe(scenario.Creditor.Id);
        result.CreditorName.ShouldBe(scenario.Creditor.Name);
        result.CreditorType.ShouldBe((Balance.Communication.Enums.CreditorType)scenario.Creditor.Type);
        result.CategoryName.ShouldBe(scenario.Category.Name);
    }

    [Fact]
    public async Task Error_A_Foreign_Id_Throws_Not_Found()
    {
        await WithInvariantCulture(async () =>
        {
            var scenario = Scenario.Build();

            var act = async () => await scenario.UseCase().Execute(Guid.NewGuid());

            var exception = await act.ShouldThrowAsync<NotFoundException>();

            exception.GetErrors().ShouldContain("Debt not found.");
        });
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

        public DebtReadOnlyRepositoryBuilder DebtRepository { get; } = new();

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

        public Debt ScheduledDebt() =>
            DebtBuilder.Build(Person, Creditor, Category, DebtMode.Scheduled);

        public Debt OpenEndedDebt(decimal? totalAmount = null)
        {
            var debt = DebtBuilder.Build(Person, Creditor, Category, DebtMode.OpenEnded);

            if (totalAmount.HasValue)
            {
                debt.TotalAmount = totalAmount.Value;
            }

            return debt;
        }

        public GetDebtByIdUseCase UseCase() =>
            new(DebtRepository.Build(), LoggedUserBuilder.Build(User));
    }
}
