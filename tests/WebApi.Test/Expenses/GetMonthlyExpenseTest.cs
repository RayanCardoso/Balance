using Balance.Communication.Enums;
using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Expenses;

public class GetMonthlyExpenseTest : BalanceClassFixture
{
    private const string EXPENSE = "api/expense";
    private const string INSTALLMENT_PLAN = "api/expense/installment-plan";
    private const string RECURRING_EXPENSE = "api/recurring-expense";
    private const string PAYMENT = "api/recurring-expense/payment";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    /// <summary>Every fixture is written into this month, and every read asks for it.</summary>
    private static readonly DateOnly August = new(2026, 8, 1);

    public GetMonthlyExpenseTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task A_Month_Reconciles_Its_Variable_And_Recurring_Lines_With_Totals()
    {
        var caller = await NewAccount();

        await NewExpense(caller, amount: 80.00m, name: "Mercado");

        var paid = await NewRecurringExpense(caller, amount: 150.00m, name: "Luz");
        await PayBill(caller, paid, amountPaid: 180.00m);

        await NewRecurringExpense(caller, amount: 45.00m, name: "Netflix");

        var month = await GetMonth(caller);

        month.GetProperty("competenceMonth").GetString().ShouldBe("2026-08-01");

        var variable = month.GetProperty("variableLines").EnumerateArray().ToList();
        var recurring = month.GetProperty("recurringLines").EnumerateArray().ToList();

        variable.ShouldHaveSingleItem().GetProperty("amount").GetDecimal().ShouldBe(80.00m);
        recurring.Count.ShouldBe(2);

        var luz = recurring.Single(line => line.GetProperty("name").GetString() == "Luz");
        luz.GetProperty("expectedAmount").GetDecimal().ShouldBe(150.00m);
        luz.GetProperty("actualAmount").GetDecimal().ShouldBe(180.00m);
        luz.GetProperty("status").GetInt32().ShouldBe((int)ExpenseStatus.Divergent);

        var netflix = recurring.Single(line => line.GetProperty("name").GetString() == "Netflix");
        netflix.GetProperty("expectedAmount").GetDecimal().ShouldBe(45.00m);
        netflix.GetProperty("actualAmount").ValueKind.ShouldBe(JsonValueKind.Null);
        netflix.GetProperty("status").GetInt32().ShouldBe((int)ExpenseStatus.Pending);

        month.GetProperty("totalVariable").GetDecimal().ShouldBe(80.00m);
        month.GetProperty("totalRecurringExpected").GetDecimal().ShouldBe(195.00m);
        month.GetProperty("totalRecurringPaid").GetDecimal().ShouldBe(180.00m);

        // 80 variable + 180 actually paid for Luz + 45 still estimated for Netflix
        month.GetProperty("totalCommitted").GetDecimal().ShouldBe(305.00m);
    }

    [Fact]
    public async Task A_Bill_Paid_At_Its_Estimate_Is_Reported_As_Paid()
    {
        var caller = await NewAccount();

        var bill = await NewRecurringExpense(caller, amount: 150.00m, name: "Luz");
        await PayBill(caller, bill, amountPaid: 150.00m);

        var month = await GetMonth(caller);

        var line = month.GetProperty("recurringLines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("actualAmount").GetDecimal().ShouldBe(150.00m);
        line.GetProperty("status").GetInt32().ShouldBe((int)ExpenseStatus.Paid);
    }

    /// <summary>
    /// RECR-05 AC3 and VIEW-02. The `Archived == false` filter lives in the repository, so this is the
    /// only layer where it actually runs - a use-case test with a mocked repository would assert what
    /// the mock was told to return, not the filter.
    /// </summary>
    [Fact]
    public async Task An_Archived_Recurring_Expense_Is_Omitted_While_Its_Payment_Survives()
    {
        var caller = await NewAccount();

        var bill = await NewRecurringExpense(caller, amount: 150.00m, name: "Academia");
        await PayBill(caller, bill, amountPaid: 150.00m);

        (await GetMonth(caller)).GetProperty("recurringLines").EnumerateArray().Count().ShouldBe(1);

        var archiveResponse = await DoPut(
            $"{RECURRING_EXPENSE}/{bill}/archive?archived=true", new { }, token: caller.Token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var archivedMonth = await GetMonth(caller);

        archivedMonth.GetProperty("recurringLines").EnumerateArray().ShouldBeEmpty();
        archivedMonth.GetProperty("totalRecurringExpected").GetDecimal().ShouldBe(0m);
        archivedMonth.GetProperty("totalCommitted").GetDecimal().ShouldBe(0m);

        // The payment was not destroyed: unarchiving brings the line back with its recorded value.
        var unarchiveResponse = await DoPut(
            $"{RECURRING_EXPENSE}/{bill}/archive?archived=false", new { }, token: caller.Token);
        unarchiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var restored = (await GetMonth(caller))
            .GetProperty("recurringLines").EnumerateArray().ShouldHaveSingleItem();

        restored.GetProperty("actualAmount").GetDecimal().ShouldBe(150.00m);
    }

    [Fact]
    public async Task An_Installment_Line_Carries_Its_Number_And_Its_Plans_Count()
    {
        var caller = await NewAccount();

        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);

        request.TotalAmount = 300.00m;
        request.InstallmentCount = 3;
        request.StartDate = new DateOnly(2026, 8, 5);

        var response = await DoPost(INSTALLMENT_PLAN, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var month = await GetMonth(caller);

        var line = month.GetProperty("variableLines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("installmentNumber").GetInt32().ShouldBe(1);
        line.GetProperty("installmentCount").GetInt32().ShouldBe(3);
        line.GetProperty("amount").GetDecimal().ShouldBe(100.00m);

        // Only installment 1 falls in August; the other two are in later competence months.
        month.GetProperty("totalVariable").GetDecimal().ShouldBe(100.00m);
    }

    /// <summary>
    /// An expense without a registered account (Pix, debit) must report the absence as null on both
    /// fields - never as an empty name, which would read as an account that simply has no name.
    /// </summary>
    [Fact]
    public async Task Variable_Line_Without_Account_Has_No_Account_Name()
    {
        var caller = await NewAccount();

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Type = ExpenseType.Pix;
        request.AccountId = null;
        request.Date = new DateOnly(2026, 8, 5);

        var response = await DoPost(EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var line = (await GetMonth(caller)).GetProperty("variableLines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("accountId").ValueKind.ShouldBe(JsonValueKind.Null);
        line.GetProperty("accountName").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>
    /// VIEW AC3's second clause. The due day reaches the page, so a wrong value is user-visible; this
    /// asserts it survives the whole round trip rather than only the use case's projection.
    /// </summary>
    [Fact]
    public async Task A_Recurring_Line_Reports_The_Due_Day_It_Was_Registered_With()
    {
        var caller = await NewAccount();

        await NewRecurringExpense(caller, amount: 2250.00m, name: "Aluguel", dueDay: 10);
        await NewRecurringExpense(caller, amount: 44.90m, name: "Netflix", dueDay: 22);

        var recurring = (await GetMonth(caller)).GetProperty("recurringLines").EnumerateArray().ToList();

        recurring.Single(line => line.GetProperty("name").GetString() == "Aluguel")
            .GetProperty("dueDay").GetInt32().ShouldBe(10);

        recurring.Single(line => line.GetProperty("name").GetString() == "Netflix")
            .GetProperty("dueDay").GetInt32().ShouldBe(22);
    }

    /// <summary>
    /// The payment id has to survive the whole round trip: without it on the monthly line, a client
    /// cannot reach PUT /api/recurring-expense/payment/{id} and a bill paid in an earlier session can
    /// never be corrected. The exact id is asserted, not the field's presence.
    /// </summary>
    [Fact]
    public async Task A_Recurring_Line_Reports_The_Id_Of_The_Payment_Recorded_For_That_Month()
    {
        var caller = await NewAccount();

        var paid = await NewRecurringExpense(caller, amount: 150.00m, name: "Luz");
        var paymentId = await PayBill(caller, paid, amountPaid: 180.00m);

        await NewRecurringExpense(caller, amount: 45.00m, name: "Netflix");

        var recurring = (await GetMonth(caller)).GetProperty("recurringLines").EnumerateArray().ToList();

        recurring.Single(line => line.GetProperty("name").GetString() == "Luz")
            .GetProperty("paymentId").GetGuid().ShouldBe(paymentId);

        recurring.Single(line => line.GetProperty("name").GetString() == "Netflix")
            .GetProperty("paymentId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>
    /// RTYP-06 and the spec's independent test: a bill's own payment type is reported by default, and
    /// one month's override reported for that month only.
    /// </summary>
    [Fact]
    public async Task A_Recurring_Lines_Type_Is_The_Months_Override_Or_The_Bills_Own_Type()
    {
        var caller = await NewAccount();

        var debit = await NewRecurringExpense(caller, amount: 150.00m, name: "Luz");
        await PayBill(caller, debit, amountPaid: 150.00m, type: ExpenseType.Pix);

        await NewRecurringExpense(caller, amount: 45.00m, name: "Netflix");

        var recurring = (await GetMonth(caller)).GetProperty("recurringLines").EnumerateArray().ToList();

        recurring.Single(line => line.GetProperty("name").GetString() == "Luz")
            .GetProperty("type").GetInt32().ShouldBe((int)ExpenseType.Pix);

        recurring.Single(line => line.GetProperty("name").GetString() == "Netflix")
            .GetProperty("type").GetInt32().ShouldBe((int)ExpenseType.Debit);
    }

    [Fact]
    public async Task The_Estimate_Flag_Is_Reported_On_The_Line()
    {
        var caller = await NewAccount();

        await NewRecurringExpense(caller, amount: 150.00m, name: "Luz", isEstimate: true);
        await NewRecurringExpense(caller, amount: 45.00m, name: "Netflix", isEstimate: false);

        var recurring = (await GetMonth(caller)).GetProperty("recurringLines").EnumerateArray().ToList();

        recurring.Single(line => line.GetProperty("name").GetString() == "Luz")
            .GetProperty("isEstimate").GetBoolean().ShouldBeTrue();

        recurring.Single(line => line.GetProperty("name").GetString() == "Netflix")
            .GetProperty("isEstimate").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task A_Second_Account_Sees_None_Of_The_First_Accounts_Month()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        await NewExpense(first, amount: 80.00m, name: "Mercado");
        await NewRecurringExpense(first, amount: 150.00m, name: "Luz");

        var month = await GetMonth(second);

        month.GetProperty("variableLines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("recurringLines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("totalCommitted").GetDecimal().ShouldBe(0m);
    }

    [Fact]
    public async Task An_Account_With_Nothing_Recorded_Returns_Empty_Collections_And_Zeroed_Totals()
    {
        var caller = await NewAccount();

        var month = await GetMonth(caller);

        month.GetProperty("variableLines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("recurringLines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("totalVariable").GetDecimal().ShouldBe(0m);
        month.GetProperty("totalRecurringExpected").GetDecimal().ShouldBe(0m);
        month.GetProperty("totalRecurringPaid").GetDecimal().ShouldBe(0m);
        month.GetProperty("totalCommitted").GetDecimal().ShouldBe(0m);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_An_Invalid_Month_Is_Rejected(string culture)
    {
        var caller = await NewAccount();

        var response = await DoGet($"{EXPENSE}/2026/13", token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.REFERENCE_MONTH_INVALID), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{EXPENSE}/2026/8");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

    private async Task<JsonElement> GetMonth(Caller caller)
    {
        var response = await DoGet($"{EXPENSE}/{August.Year}/{August.Month}", token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await ReadJson(response);
    }

    private async Task<Caller> NewAccount()
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var people = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().ToList();

        var personId = people[0].GetProperty("id").GetGuid();

        var categoryResponse = await DoPost(CATEGORY, RequestRegisterCategoryJsonBuilder.Build(), token: token);
        categoryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Closing day 28 keeps every fixture dated the 5th inside its own competence month.
        var accountRequest = RequestRegisterAccountJsonBuilder.Build(personId);
        accountRequest.ClosingDay = 28;

        var accountResponse = await DoPost(ACCOUNT, accountRequest, token: token);
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return new Caller(
            token,
            personId,
            (await ReadJson(categoryResponse)).GetProperty("id").GetGuid(),
            (await ReadJson(accountResponse)).GetProperty("id").GetGuid());
    }

    private async Task NewExpense(Caller caller, decimal amount, string name)
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);

        request.Name = name;
        request.Amount = amount;
        request.Date = new DateOnly(2026, 8, 5);

        var response = await DoPost(EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private async Task<Guid> NewRecurringExpense(
        Caller caller, decimal amount, string name, bool isEstimate = true, int? dueDay = null)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);

        request.Name = name;
        request.Amount = amount;
        request.IsEstimate = isEstimate;
        request.ValidityStart = new DateOnly(2026, 1, 1);

        if (dueDay is not null)
        {
            request.DueDay = dueDay.Value;
        }

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    /// <summary>Returns the created payment's id so a test can pin the value the line reports.</summary>
    private async Task<Guid> PayBill(
        Caller caller, Guid recurringExpenseId, decimal amountPaid, ExpenseType? type = null)
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            recurringExpenseId, referenceMonth: August);

        request.AmountPaid = amountPaid;
        request.Type = type;

        var response = await DoPost(PAYMENT, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
