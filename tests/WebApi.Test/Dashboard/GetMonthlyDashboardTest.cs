using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Dashboard;

public class GetMonthlyDashboardTest : BalanceClassFixture
{
    private const string DASHBOARD = "api/dashboard";
    private const string INCOME = "api/income";
    private const string INCOME_PAYMENT = "api/income/payment";
    private const string EXPENSE = "api/expense";
    private const string RECURRING_EXPENSE = "api/RecurringExpense";
    private const string PAYMENT = "api/RecurringExpense/payment";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    private static readonly DateOnly August = new(2026, 8, 1);

    public GetMonthlyDashboardTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// DASH-01 and the feature's success criterion that the dashboard's income half is identical to
    /// what the income endpoint returns. Comparing the serialised JSON, rather than a field or two,
    /// is what makes this fail if the dashboard ever reshapes, filters or recomputes income.
    /// </summary>
    [Fact]
    public async Task Both_Halves_Match_What_The_Individual_Endpoints_Return()
    {
        var caller = await NewAccount();

        await NewIncomeWithPayment(caller, amountReceived: 5000.00m);
        await NewExpense(caller, amount: 80.00m);

        var bill = await NewRecurringExpense(caller, amount: 150.00m);
        await PayBill(caller, bill, amountPaid: 180.00m);

        var dashboard = await GetDashboard(caller);

        var income = await ReadJson(await DoGet($"{INCOME}/2026/8", token: caller.Token));
        var expenses = await ReadJson(await DoGet($"{EXPENSE}/2026/8", token: caller.Token));

        dashboard.GetProperty("income").GetRawText().ShouldBe(income.GetRawText());
        dashboard.GetProperty("expenses").GetRawText().ShouldBe(expenses.GetRawText());
    }

    [Fact]
    public async Task The_Balance_Is_The_Income_Received_Minus_The_Committed_Expense()
    {
        var caller = await NewAccount();

        await NewIncomeWithPayment(caller, amountReceived: 5000.00m);
        await NewExpense(caller, amount: 80.00m);

        var paid = await NewRecurringExpense(caller, amount: 150.00m, name: "Luz");
        await PayBill(caller, paid, amountPaid: 180.00m);

        await NewRecurringExpense(caller, amount: 45.00m, name: "Netflix");

        var dashboard = await GetDashboard(caller);

        // 80 variable + 180 paid for Luz + 45 still estimated for Netflix
        dashboard.GetProperty("expenses").GetProperty("totalCommitted").GetDecimal().ShouldBe(305.00m);
        dashboard.GetProperty("income").GetProperty("totalReceived").GetDecimal().ShouldBe(5000.00m);

        dashboard.GetProperty("balance").GetDecimal().ShouldBe(4695.00m);
        dashboard.GetProperty("competenceMonth").GetString().ShouldBe("2026-08-01");
    }

    [Fact]
    public async Task A_Month_Costing_More_Than_It_Earned_Reports_A_Negative_Balance()
    {
        var caller = await NewAccount();

        await NewIncomeWithPayment(caller, amountReceived: 100.00m);
        await NewExpense(caller, amount: 400.00m);

        var dashboard = await GetDashboard(caller);

        dashboard.GetProperty("balance").GetDecimal().ShouldBe(-300.00m);
    }

    [Fact]
    public async Task A_Second_Account_Sees_None_Of_The_First_Accounts_Month()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        await NewIncomeWithPayment(first, amountReceived: 5000.00m);
        await NewExpense(first, amount: 80.00m);

        var dashboard = await GetDashboard(second);

        dashboard.GetProperty("income").GetProperty("lines").EnumerateArray().ShouldBeEmpty();
        dashboard.GetProperty("expenses").GetProperty("variableLines").EnumerateArray().ShouldBeEmpty();
        dashboard.GetProperty("expenses").GetProperty("recurringLines").EnumerateArray().ShouldBeEmpty();
        dashboard.GetProperty("balance").GetDecimal().ShouldBe(0m);
    }

    [Fact]
    public async Task An_Empty_Account_Returns_A_Zero_Balance()
    {
        var caller = await NewAccount();

        var dashboard = await GetDashboard(caller);

        dashboard.GetProperty("balance").GetDecimal().ShouldBe(0m);
        dashboard.GetProperty("income").GetProperty("totalReceived").GetDecimal().ShouldBe(0m);
        dashboard.GetProperty("expenses").GetProperty("totalCommitted").GetDecimal().ShouldBe(0m);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_An_Invalid_Month_Is_Rejected(string culture)
    {
        var caller = await NewAccount();

        var response = await DoGet($"{DASHBOARD}/2026/13", token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.REFERENCE_MONTH_INVALID), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{DASHBOARD}/2026/8");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

    private async Task<JsonElement> GetDashboard(Caller caller)
    {
        var response = await DoGet($"{DASHBOARD}/2026/8", token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await ReadJson(response);
    }

    private async Task<Caller> NewAccount()
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var personId = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().First().GetProperty("id").GetGuid();

        var categoryResponse = await DoPost(CATEGORY, RequestRegisterCategoryJsonBuilder.Build(), token: token);
        categoryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

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

    private async Task NewIncomeWithPayment(Caller caller, decimal amountReceived)
    {
        var sourceRequest = RequestRegisterIncomeSourceJsonBuilder.Recurring(caller.PersonId);
        sourceRequest.Amount = amountReceived;

        var sourceResponse = await DoPost(INCOME, sourceRequest, token: caller.Token);
        sourceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var sourceId = (await ReadJson(sourceResponse)).GetProperty("id").GetGuid();

        var paymentRequest = RequestRegisterIncomePaymentJsonBuilder.Build(sourceId, referenceMonth: August);
        paymentRequest.AmountReceived = amountReceived;

        var paymentResponse = await DoPost(INCOME_PAYMENT, paymentRequest, token: caller.Token);
        paymentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private async Task NewExpense(Caller caller, decimal amount)
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);

        request.Amount = amount;
        request.Date = new DateOnly(2026, 8, 5);

        var response = await DoPost(EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private async Task<Guid> NewRecurringExpense(Caller caller, decimal amount, string name = "Luz")
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);

        request.Name = name;
        request.Amount = amount;
        request.ValidityStart = new DateOnly(2026, 1, 1);

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    private async Task PayBill(Caller caller, Guid recurringExpenseId, decimal amountPaid)
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            recurringExpenseId, referenceMonth: August);

        request.AmountPaid = amountPaid;

        var response = await DoPost(PAYMENT, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
