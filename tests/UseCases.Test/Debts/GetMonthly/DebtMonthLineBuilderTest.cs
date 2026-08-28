using Balance.Application.UseCases.Debts.GetMonthly;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using CommonTestUtilities.Entities;
using Shouldly;
using CommunicationExpenseStatus = Balance.Communication.Enums.ExpenseStatus;

namespace UseCases.Test.Debts.GetMonthly;

public class DebtMonthLineBuilderTest
{
    // Fixed rather than read from the clock: an overdue assertion driven by DateTime.UtcNow would
    // pass today and fail next month. Every test below controls "now" through this constant and
    // the fixture's due dates instead.
    private static readonly DateOnly Today = new(2026, 8, 15);

    [Fact]
    public void BuildScheduled_An_Installment_With_No_Payment_Is_Pending_With_A_Null_AmountPaid()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(5), expectedAmount: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.Status.ShouldBe(CommunicationExpenseStatus.Pending);
        line.AmountPaid.ShouldBeNull();
    }

    /// <summary>
    /// The id, not just the number. `POST api/Debt/payment` identifies a scheduled payment by
    /// installment id, so a client that only had the number would have to fetch the whole debt
    /// before it could pay a line it is already looking at.
    /// </summary>
    [Fact]
    public void BuildScheduled_Carries_The_Installment_Id_So_The_Line_Can_Be_Paid_Directly()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(5), expectedAmount: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.InstallmentId.ShouldBe(installment.Id);
    }

    [Fact]
    public void BuildScheduled_A_Payment_Equal_To_The_Expected_Amount_Is_Paid()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(5), expectedAmount: 100m);
        var payment = DebtPaymentBuilder.Build(debt, debtInstallmentId: installment.Id, amountPaid: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment, today: Today);

        line.Status.ShouldBe(CommunicationExpenseStatus.Paid);
        line.AmountPaid.ShouldBe(100m);
    }

    [Fact]
    public void BuildScheduled_A_Payment_Different_From_The_Expected_Amount_Is_Divergent()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(5), expectedAmount: 100m);
        var payment = DebtPaymentBuilder.Build(debt, debtInstallmentId: installment.Id, amountPaid: 80m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment, today: Today);

        line.Status.ShouldBe(CommunicationExpenseStatus.Divergent);
    }

    [Fact]
    public void BuildScheduled_A_Pending_Line_Whose_Due_Date_Is_Before_Today_Is_Overdue()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(-1), expectedAmount: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.Status.ShouldBe(CommunicationExpenseStatus.Pending);
        line.IsOverdue.ShouldBeTrue();
    }

    [Fact]
    public void BuildScheduled_The_Same_Overdue_Line_With_A_Payment_Is_Not_Overdue()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today.AddDays(-1), expectedAmount: 100m);
        var payment = DebtPaymentBuilder.Build(debt, debtInstallmentId: installment.Id, amountPaid: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment, today: Today);

        line.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public void BuildScheduled_A_Pending_Line_Whose_Due_Date_Is_Exactly_Today_Is_Not_Overdue()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, dueDate: Today, expectedAmount: 100m);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.Status.ShouldBe(CommunicationExpenseStatus.Pending);
        line.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public void BuildScheduled_The_Creditor_Name_Type_And_Category_Name_Are_Carried_Through()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.CreditorName.ShouldBe(scenario.Creditor.Name);
        line.CreditorType.ShouldBe((Balance.Communication.Enums.CreditorType)scenario.Creditor.Type);
        line.CategoryName.ShouldBe(scenario.Category.Name);
    }

    /// <summary>
    /// FINDING 3 (DVEW-01 AC1): the monthly line must carry the payment's type and account name,
    /// not just its date and notes.
    /// </summary>
    [Fact]
    public void BuildScheduled_The_Payment_Type_And_Account_Are_Carried_Through()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt, expectedAmount: 100m);
        var account = AccountBuilder.Build(scenario.Person);
        var payment = DebtPaymentBuilder.Build(
            debt, debtInstallmentId: installment.Id, amountPaid: 100m, accountId: account.Id,
            type: ExpenseType.Pix);
        payment.Account = account;

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment, today: Today);

        line.Type.ShouldBe(Balance.Communication.Enums.ExpenseType.Pix);
        line.AccountId.ShouldBe(account.Id);
        line.AccountName.ShouldBe(account.Name);
    }

    /// <summary>
    /// A Scheduled installment that has not been paid yet has no type or account to report.
    /// </summary>
    [Fact]
    public void BuildScheduled_With_No_Payment_Has_A_Null_Type_And_Account()
    {
        var scenario = Scenario.Build();
        var debt = scenario.ScheduledDebt();
        var installment = DebtInstallmentBuilder.Build(debt);

        var line = DebtMonthLineBuilder.BuildScheduled(debt, installment, payment: null, today: Today);

        line.Type.ShouldBeNull();
        line.AccountId.ShouldBeNull();
        line.AccountName.ShouldBeNull();
    }

    [Fact]
    public void BuildOpenEnded_Carries_A_Null_ExpectedAmount_A_Null_InstallmentNumber_Paid_And_Not_Overdue()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        var payment = DebtPaymentBuilder.Build(debt, amountPaid: 250m);

        var line = DebtMonthLineBuilder.BuildOpenEnded(debt, payment);

        line.ExpectedAmount.ShouldBeNull();
        line.InstallmentId.ShouldBeNull();
        line.InstallmentNumber.ShouldBeNull();
        line.Status.ShouldBe(CommunicationExpenseStatus.Paid);
        line.IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public void BuildOpenEnded_The_Creditor_Name_Type_And_Category_Name_Are_Carried_Through()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        var payment = DebtPaymentBuilder.Build(debt, amountPaid: 250m);

        var line = DebtMonthLineBuilder.BuildOpenEnded(debt, payment);

        line.CreditorName.ShouldBe(scenario.Creditor.Name);
        line.CreditorType.ShouldBe((Balance.Communication.Enums.CreditorType)scenario.Creditor.Type);
        line.CategoryName.ShouldBe(scenario.Category.Name);
    }

    /// <summary>
    /// FINDING 3 (DVEW-01 AC1): the monthly line must carry the payment's type and account name,
    /// not just its date and notes.
    /// </summary>
    [Fact]
    public void BuildOpenEnded_The_Payment_Type_And_Account_Are_Carried_Through()
    {
        var scenario = Scenario.Build();
        var debt = scenario.OpenEndedDebt();
        var account = AccountBuilder.Build(scenario.Person);
        var payment = DebtPaymentBuilder.Build(
            debt, amountPaid: 250m, accountId: account.Id, type: ExpenseType.Credit);
        payment.Account = account;

        var line = DebtMonthLineBuilder.BuildOpenEnded(debt, payment);

        line.Type.ShouldBe(Balance.Communication.Enums.ExpenseType.Credit);
        line.AccountId.ShouldBe(account.Id);
        line.AccountName.ShouldBe(account.Name);
    }

    [Fact]
    public void ResolveStatus_A_Null_Actual_Is_Pending_Regardless_Of_The_Expected_Amount()
    {
        DebtMonthLineBuilder.ResolveStatus(null, null).ShouldBe(CommunicationExpenseStatus.Pending);
        DebtMonthLineBuilder.ResolveStatus(100m, null).ShouldBe(CommunicationExpenseStatus.Pending);
    }

    [Fact]
    public void ResolveStatus_A_Null_Expected_With_An_Actual_Is_Paid()
    {
        DebtMonthLineBuilder.ResolveStatus(null, 100m).ShouldBe(CommunicationExpenseStatus.Paid);
    }

    [Fact]
    public void ResolveStatus_An_Equal_Amount_Is_Paid_And_A_Different_Amount_Is_Divergent()
    {
        DebtMonthLineBuilder.ResolveStatus(100m, 100m).ShouldBe(CommunicationExpenseStatus.Paid);
        DebtMonthLineBuilder.ResolveStatus(100m, 80m).ShouldBe(CommunicationExpenseStatus.Divergent);
    }

    private sealed class Scenario
    {
        public required User User { get; init; }
        public required Person Person { get; init; }
        public required Creditor Creditor { get; init; }
        public required Category Category { get; init; }

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

        public Debt OpenEndedDebt() =>
            DebtBuilder.Build(Person, Creditor, Category, DebtMode.OpenEnded);
    }
}
