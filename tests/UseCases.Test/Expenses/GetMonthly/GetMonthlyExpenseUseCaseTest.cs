using Balance.Application.UseCases.Expenses.GetMonthly;
using Balance.Communication.Enums;
using Balance.Domain.Entities;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommonTestUtilities.Entities;
using CommonTestUtilities.LoggedUser;
using CommonTestUtilities.Repositories;
using Shouldly;
using DomainExpenseType = Balance.Domain.Enums.ExpenseType;

namespace UseCases.Test.Expenses.GetMonthly;

public class GetMonthlyExpenseUseCaseTest
{
    private static readonly DateOnly August = new(2026, 8, 1);

    [Fact]
    public async Task A_Bill_Paid_At_Its_Estimate_Is_Paid()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m);
        Pay(bill, August, 150m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        var line = result.RecurringLines.ShouldHaveSingleItem();

        line.ExpectedAmount.ShouldBe(150m);
        line.ActualAmount.ShouldBe(150m);
        line.Status.ShouldBe(ExpenseStatus.Paid);
    }

    [Fact]
    public async Task A_Bill_Paid_Above_Its_Estimate_Is_Divergent_And_Reports_The_Real_Value()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m);
        Pay(bill, August, 180m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        var line = result.RecurringLines.ShouldHaveSingleItem();

        line.ExpectedAmount.ShouldBe(150m);
        line.ActualAmount.ShouldBe(180m);
        line.Status.ShouldBe(ExpenseStatus.Divergent);
    }

    [Fact]
    public async Task A_Paid_Lines_Type_Is_The_Months_Override_When_Present()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m, type: DomainExpenseType.Debit);
        Pay(bill, August, 150m, type: DomainExpenseType.Pix);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.ShouldHaveSingleItem().Type.ShouldBe(ExpenseType.Pix);
    }

    [Fact]
    public async Task A_Lines_Type_Falls_Back_To_The_Recurring_Expenses_Own_Type_When_No_Override_Is_Recorded()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m, type: DomainExpenseType.Debit);
        Pay(bill, August, 150m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.ShouldHaveSingleItem().Type.ShouldBe(ExpenseType.Debit);
    }

    [Fact]
    public async Task An_Unpaid_Bill_Is_Pending_With_A_Null_Actual()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        var line = result.RecurringLines.ShouldHaveSingleItem();

        line.ExpectedAmount.ShouldBe(150m);
        line.ActualAmount.ShouldBeNull();
        line.PaymentDate.ShouldBeNull();
        line.Status.ShouldBe(ExpenseStatus.Pending);
    }

    /// <summary>
    /// The monthly line is the only place a client can read the payment's id from, and
    /// <c>PUT /api/recurring-expense/payment/{id}</c> needs it to correct a bill paid earlier. The id
    /// value itself is asserted, not the field's presence - the wrong due day shipped exactly that way.
    /// </summary>
    [Fact]
    public async Task A_Paid_Line_Carries_The_Id_Of_That_Months_Payment()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m);
        var paymentId = Pay(bill, August, 180m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.ShouldHaveSingleItem().PaymentId.ShouldBe(paymentId);
    }

    [Fact]
    public async Task An_Unpaid_Line_Carries_A_Null_Payment_Id()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m);

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.ShouldHaveSingleItem().PaymentId.ShouldBeNull();
    }

    /// <summary>
    /// VIEW AC3's second clause: the line reports the recurring expense's due day. It is rendered on
    /// the page, so a wrong value is user-visible.
    /// </summary>
    [Fact]
    public async Task A_Recurring_Line_Reports_The_Due_Day_Of_Its_Expense()
    {
        var (user, person) = NewOwner();

        var rent = RecurringExpenseBuilder.Build(person, dueDay: 10, name: "Aluguel");
        var netflix = RecurringExpenseBuilder.Build(person, dueDay: 22, name: "Netflix");

        var useCase = BuildUseCase(user, recurringExpenses: [rent, netflix]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.First(line => line.Name == "Aluguel").DueDay.ShouldBe(10);
        result.RecurringLines.First(line => line.Name == "Netflix").DueDay.ShouldBe(22);
    }

    /// <summary>
    /// Spec edge case: a due day of 31 in a shorter month is reported as stored, never clamped to the
    /// month's length.
    /// </summary>
    [Fact]
    public async Task A_Due_Day_Of_31_Is_Not_Clamped_To_A_Shorter_Month()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, dueDay: 31);

        // February 2026 has 28 days.
        var useCase = BuildUseCase(user, competenceMonth: new DateOnly(2026, 2, 1), recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 2);

        result.RecurringLines.ShouldHaveSingleItem().DueDay.ShouldBe(31);
    }

    /// <summary>
    /// The status rule's remaining branch: money went out for a month that has no version in effect,
    /// so there is no expectation to diverge from. Not reachable through the public API today -
    /// registering a payment rejects that month with NO_VERSION_IN_EFFECT - but the branch exists and
    /// is pinned here so a future operation cannot change its meaning unnoticed.
    /// </summary>
    [Fact]
    public async Task A_Payment_With_No_Version_In_Effect_Is_Paid_With_A_Null_Expected()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m, validityStart: new DateOnly(2026, 5, 1));
        Pay(bill, new DateOnly(2026, 2, 1), 90m);

        var useCase = BuildUseCase(user, competenceMonth: new DateOnly(2026, 2, 1), recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 2);

        var line = result.RecurringLines.ShouldHaveSingleItem();

        line.ExpectedAmount.ShouldBeNull();
        line.ActualAmount.ShouldBe(90m);
        line.Status.ShouldBe(ExpenseStatus.Paid);
    }

    [Fact]
    public async Task A_Month_Predating_Every_Version_Reports_A_Null_Expected_Amount()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m, validityStart: new DateOnly(2026, 5, 1));

        var useCase = BuildUseCase(user, competenceMonth: new DateOnly(2026, 2, 1), recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 2);

        var line = result.RecurringLines.ShouldHaveSingleItem();

        line.ExpectedAmount.ShouldBeNull();
        line.Status.ShouldBe(ExpenseStatus.Pending);
    }

    [Fact]
    public async Task A_Month_Inside_A_Closed_Version_Reports_That_Versions_Amount()
    {
        var (user, person) = NewOwner();

        var bill = RecurringExpenseBuilder.Build(person, amount: 150m, validityStart: new DateOnly(2026, 1, 1));

        bill.Versions[0].ValidityEnd = new DateOnly(2026, 8, 31);
        bill.Versions.Add(new RecurringExpenseVersion
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = bill.Id,
            Amount = 180m,
            ValidityStart = new DateOnly(2026, 9, 1),
            ValidityEnd = null,
            ChangeReason = "reajuste anual"
        });

        var useCase = BuildUseCase(user, recurringExpenses: [bill]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.ShouldHaveSingleItem().ExpectedAmount.ShouldBe(150m);
    }

    [Fact]
    public async Task The_Estimate_Flag_Of_The_Recurring_Expense_Is_Reported()
    {
        var (user, person) = NewOwner();

        var estimated = RecurringExpenseBuilder.Build(person, isEstimate: true, name: "Luz");
        var fixedValue = RecurringExpenseBuilder.Build(person, isEstimate: false, name: "Netflix");

        var useCase = BuildUseCase(user, recurringExpenses: [estimated, fixedValue]);

        var result = await useCase.Execute(2026, 8);

        result.RecurringLines.First(line => line.Name == "Luz").IsEstimate.ShouldBeTrue();
        result.RecurringLines.First(line => line.Name == "Netflix").IsEstimate.ShouldBeFalse();
    }

    [Fact]
    public async Task An_Installment_Line_Carries_Its_Number_And_Its_Plans_Count()
    {
        var (user, person) = NewOwner();

        var plan = new InstallmentPlan
        {
            Id = Guid.NewGuid(),
            Name = "Geladeira",
            TotalAmount = 3000m,
            InstallmentCount = 10,
            PersonId = person.Id
        };

        var installment = NewExpense(person, amount: 300m, name: "Geladeira");
        installment.InstallmentNumber = 3;
        installment.InstallmentPlanId = plan.Id;
        installment.InstallmentPlan = plan;

        var useCase = BuildUseCase(user, expenses: [installment]);

        var result = await useCase.Execute(2026, 8);

        var line = result.VariableLines.ShouldHaveSingleItem();

        line.InstallmentNumber.ShouldBe(3);
        line.InstallmentCount.ShouldBe(10);
        line.InstallmentPlanId.ShouldBe(plan.Id);
    }

    [Fact]
    public async Task A_One_Off_Expense_Carries_No_Installment_Markers()
    {
        var (user, person) = NewOwner();

        var useCase = BuildUseCase(user, expenses: [NewExpense(person, amount: 80m, name: "Mercado")]);

        var result = await useCase.Execute(2026, 8);

        var line = result.VariableLines.ShouldHaveSingleItem();

        line.InstallmentNumber.ShouldBeNull();
        line.InstallmentCount.ShouldBeNull();
        line.InstallmentPlanId.ShouldBeNull();
    }

    [Fact]
    public async Task The_Committed_Total_Uses_The_Actual_When_Paid_And_The_Estimate_When_Not()
    {
        var (user, person) = NewOwner();

        var paid = RecurringExpenseBuilder.Build(person, amount: 150m, name: "Luz");
        Pay(paid, August, 180m);

        var unpaid = RecurringExpenseBuilder.Build(person, amount: 45m, name: "Netflix");

        var useCase = BuildUseCase(
            user,
            expenses: [NewExpense(person, amount: 80m, name: "Mercado")],
            recurringExpenses: [paid, unpaid]);

        var result = await useCase.Execute(2026, 8);

        result.TotalVariable.ShouldBe(80m);
        result.TotalRecurringExpected.ShouldBe(195m);
        result.TotalRecurringPaid.ShouldBe(180m);

        // 80 variable + 180 actually paid for Luz + 45 still estimated for Netflix
        result.TotalCommitted.ShouldBe(305m);
    }

    [Fact]
    public async Task A_Month_With_Nothing_Recorded_Returns_Empty_Collections_And_Zeroed_Totals()
    {
        var (user, _) = NewOwner();

        var useCase = BuildUseCase(user);

        var result = await useCase.Execute(2026, 8);

        result.CompetenceMonth.ShouldBe(August);
        result.VariableLines.ShouldBeEmpty();
        result.RecurringLines.ShouldBeEmpty();
        result.TotalVariable.ShouldBe(0m);
        result.TotalRecurringExpected.ShouldBe(0m);
        result.TotalRecurringPaid.ShouldBe(0m);
        result.TotalCommitted.ShouldBe(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task An_Invalid_Month_Is_Rejected(int month)
    {
        var (user, _) = NewOwner();

        var useCase = BuildUseCase(user);

        var exception = await Should.ThrowAsync<ErrorOnValidationException>(
            async () => await useCase.Execute(2026, month));

        exception.GetErrors().ShouldHaveSingleItem()
            .ShouldBe(ResourceErrorMessages.REFERENCE_MONTH_INVALID);
    }

    private static (User User, Person Person) NewOwner()
    {
        var user = UserBuilder.Build();
        var person = PersonBuilder.Build(user);

        return (user, person);
    }

    private static Expense NewExpense(Person person, decimal amount, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = DomainExpenseType.Credit,
            Amount = amount,
            Date = new DateOnly(2026, 8, 5),
            CompetenceMonth = August,
            PersonId = person.Id,
            Person = person,
            CategoryId = Guid.NewGuid(),
            AccountId = Guid.NewGuid()
        };

    /// <summary>Returns the recorded payment's id so a test can pin the value the line reports.</summary>
    private static Guid Pay(
        RecurringExpense expense, DateOnly referenceMonth, decimal amountPaid, DomainExpenseType? type = null)
    {
        var payment = new RecurringExpensePayment
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = expense.Id,
            RecurringExpenseVersionId = expense.Versions[0].Id,
            ReferenceMonth = referenceMonth,
            PaymentDate = referenceMonth.AddDays(11),
            AmountPaid = amountPaid,
            Type = type
        };

        expense.Payments.Add(payment);

        return payment.Id;
    }

    private static GetMonthlyExpenseUseCase BuildUseCase(
        User user,
        DateOnly? competenceMonth = null,
        List<Expense>? expenses = null,
        List<RecurringExpense>? recurringExpenses = null)
    {
        var month = competenceMonth ?? August;

        var expenseRepository = new ExpenseReadOnlyRepositoryBuilder()
            .GetForMonth(user, month, expenses ?? [])
            .Build();

        var recurringRepository = new RecurringExpenseReadOnlyRepositoryBuilder()
            .GetForMonth(user, month, recurringExpenses ?? [])
            .Build();

        return new GetMonthlyExpenseUseCase(
            expenseRepository,
            recurringRepository,
            LoggedUserBuilder.Build(user));
    }
}
