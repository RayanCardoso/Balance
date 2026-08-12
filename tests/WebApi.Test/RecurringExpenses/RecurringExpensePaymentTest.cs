using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.RecurringExpenses;

public class RecurringExpensePaymentTest : BalanceClassFixture
{
    private const string RECURRING_EXPENSE = "api/recurring-expense";
    private const string PAYMENT = "api/recurring-expense/payment";
    private const string CHANGE_VALUE = "api/recurring-expense/value";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RecurringExpensePaymentTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Register_Returns_201_With_The_Month_Date_Amount_Notes_And_Paying_Account()
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller, amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: new DateOnly(2026, 8, 23), accountId: caller.AccountId);
        request.AmountPaid = 180.00m;
        request.PaymentDate = new DateOnly(2026, 8, 12);
        request.Notes = "bill arrived higher";

        var response = await DoPost(PAYMENT, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("recurringExpenseId").GetGuid().ShouldBe(expense.Id);
        body.GetProperty("referenceMonth").GetString().ShouldBe("2026-08-01");
        body.GetProperty("paymentDate").GetString().ShouldBe("2026-08-12");
        body.GetProperty("amountPaid").GetDecimal().ShouldBe(180.00m);
        body.GetProperty("notes").GetString().ShouldBe("bill arrived higher");
        body.GetProperty("accountId").GetGuid().ShouldBe(caller.AccountId);
        body.GetProperty("recurringExpenseVersionId").GetGuid().ShouldBe(expense.VersionId);
    }

    [Fact]
    public async Task A_Payment_For_A_Month_Before_A_Value_Change_Freezes_The_Old_Version()
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller, amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));

        var changeRequest = RequestChangeRecurringExpenseValueJsonBuilder.Build(
            expense.Id, new DateOnly(2026, 9, 1));
        changeRequest.Amount = 180.00m;

        var changeResponse = await DoPut(CHANGE_VALUE, changeRequest, token: caller.Token);
        changeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versions = (await ReadJson(changeResponse)).GetProperty("versions").EnumerateArray().ToList();
        versions.Count.ShouldBe(2);

        var oldVersionId = versions[0].GetProperty("id").GetGuid();
        var newVersionId = versions[1].GetProperty("id").GetGuid();

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: new DateOnly(2026, 8, 1));

        var response = await DoPost(PAYMENT, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var frozen = (await ReadJson(response)).GetProperty("recurringExpenseVersionId").GetGuid();

        frozen.ShouldBe(oldVersionId);
        frozen.ShouldNotBe(newVersionId);
    }

    [Fact]
    public async Task Register_Accepts_A_Null_Notes_And_A_Null_Paying_Account()
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(expense.Id);
        request.Notes = null;
        request.AccountId = null;

        var response = await DoPost(PAYMENT, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("notes").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("accountId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_Returns_200_Overwriting_The_Amount_Without_Moving_The_Month_Or_The_Version()
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller, amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));

        var registerRequest = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: new DateOnly(2026, 8, 1));
        registerRequest.AmountPaid = 180.00m;

        var registerResponse = await DoPost(PAYMENT, registerRequest, token: caller.Token);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var recorded = await ReadJson(registerResponse);
        var paymentId = recorded.GetProperty("id").GetGuid();

        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(
            amountPaid: 172.40m, paymentDate: new DateOnly(2026, 8, 15), accountId: caller.AccountId);
        request.Notes = "corrected after the bill was re-read";

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldBe(paymentId);
        body.GetProperty("amountPaid").GetDecimal().ShouldBe(172.40m);
        body.GetProperty("paymentDate").GetString().ShouldBe("2026-08-15");
        body.GetProperty("notes").GetString().ShouldBe("corrected after the bill was re-read");
        body.GetProperty("accountId").GetGuid().ShouldBe(caller.AccountId);

        body.GetProperty("referenceMonth").GetString().ShouldBe("2026-08-01");
        body.GetProperty("recurringExpenseVersionId").GetGuid()
            .ShouldBe(recorded.GetProperty("recurringExpenseVersionId").GetGuid());
        body.GetProperty("recurringExpenseVersionId").GetGuid().ShouldBe(expense.VersionId);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_A_Payment_Already_Exists_For_That_Month(string culture)
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: new DateOnly(2026, 8, 1));

        var first = await DoPost(PAYMENT, request, token: caller.Token);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await DoPost(PAYMENT, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.PAYMENT_ALREADY_RECORDED), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_The_Recurring_Expense_Is_Archived(string culture)
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller);

        var archiveResponse = await DoPut(
            $"{RECURRING_EXPENSE}/{expense.Id}/archive?archived=true", new { }, token: caller.Token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(expense.Id);

        var response = await DoPost(PAYMENT, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.RECURRING_EXPENSE_ARCHIVED), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_No_Version_In_Effect_At_The_Reference_Month(string culture)
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller, validityStart: new DateOnly(2026, 5, 1));

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: new DateOnly(2026, 2, 1));

        var response = await DoPost(PAYMENT, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.NO_VERSION_IN_EFFECT), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Register_Amount_Not_Greater_Than_Zero(string culture)
    {
        var caller = await NewAccount();
        var expense = await NewRecurringExpense(caller);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(expense.Id);
        request.AmountPaid = 0;

        var response = await DoPost(PAYMENT, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Update_Amount_Not_Greater_Than_Zero(string culture)
    {
        var caller = await NewAccount();
        var paymentId = await NewPayment(caller);

        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 0);

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), culture);
    }

    [Fact]
    public async Task Recording_A_Payment_For_Another_Users_Recurring_Expense_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var expense = await NewRecurringExpense(first);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(expense.Id);

        var response = await DoPost(PAYMENT, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Correcting_Another_Users_Payment_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var paymentId = await NewPayment(first);

        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m);

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_Payment_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(Guid.NewGuid());

        var response = await DoPost(PAYMENT, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Payment_Without_Token_Is_Unauthorized()
    {
        var request = RequestUpdateRecurringExpensePaymentJsonBuilder.Build(amountPaid: 172.40m);

        var response = await DoPut($"{PAYMENT}/{Guid.NewGuid()}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

    private sealed record RegisteredExpense(Guid Id, Guid VersionId);

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

        var accountResponse = await DoPost(ACCOUNT, RequestRegisterAccountJsonBuilder.Build(personId), token: token);
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return new Caller(
            token,
            personId,
            (await ReadJson(categoryResponse)).GetProperty("id").GetGuid(),
            (await ReadJson(accountResponse)).GetProperty("id").GetGuid());
    }

    private async Task<RegisteredExpense> NewRecurringExpense(
        Caller caller, decimal amount = 150.00m, DateOnly? validityStart = null)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Amount = amount;
        request.ValidityStart = validityStart ?? new DateOnly(2026, 1, 1);

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);
        var version = body.GetProperty("versions").EnumerateArray().ShouldHaveSingleItem();

        return new RegisteredExpense(
            body.GetProperty("id").GetGuid(),
            version.GetProperty("id").GetGuid());
    }

    private async Task<Guid> NewPayment(Caller caller, DateOnly? referenceMonth = null)
    {
        var expense = await NewRecurringExpense(caller);

        var request = RequestRegisterRecurringExpensePaymentJsonBuilder.Build(
            expense.Id, referenceMonth: referenceMonth ?? new DateOnly(2026, 8, 1));
        request.AmountPaid = 180.00m;

        var response = await DoPost(PAYMENT, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    private static async Task ShouldCarrySingleError(HttpResponseMessage response, string key, string culture)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(key, new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
