using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.RecurringExpenses;

public class RecurringExpenseEndpointsTest : BalanceClassFixture
{
    private const string RECURRING_EXPENSE = "api/recurring-expense";
    private const string CHANGE_VALUE = "api/recurring-expense/value";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RecurringExpenseEndpointsTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Register_Returns_201_With_Exactly_One_Open_Version()
    {
        var caller = await NewAccount();

        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Name = "Netflix";
        request.Amount = 55.90m;
        request.DueDay = 12;
        request.IsEstimate = true;
        request.ValidityStart = new DateOnly(2026, 1, 1);

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("name").GetString().ShouldBe("Netflix");
        body.GetProperty("personId").GetGuid().ShouldBe(caller.PersonId);
        body.GetProperty("categoryId").GetGuid().ShouldBe(caller.CategoryId);
        body.GetProperty("accountId").GetGuid().ShouldBe(caller.AccountId);
        body.GetProperty("dueDay").GetInt32().ShouldBe(12);
        body.GetProperty("isEstimate").GetBoolean().ShouldBeTrue();
        body.GetProperty("archived").GetBoolean().ShouldBeFalse();

        var version = body.GetProperty("versions").EnumerateArray().ShouldHaveSingleItem();

        version.GetProperty("amount").GetDecimal().ShouldBe(55.90m);
        version.GetProperty("validityStart").GetString().ShouldBe("2026-01-01");
        version.GetProperty("validityEnd").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Change_Value_Returns_200_With_The_Old_Version_Closed_The_Day_Before()
    {
        var caller = await NewAccount();
        var recurringExpenseId = await NewRecurringExpense(caller, amount: 150.00m, validityStart: new DateOnly(2026, 1, 1));

        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(
            recurringExpenseId, new DateOnly(2026, 9, 1));
        request.Amount = 180.00m;
        request.ChangeReason = "tariff increase";

        var response = await DoPut(CHANGE_VALUE, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versions = (await ReadJson(response)).GetProperty("versions").EnumerateArray().ToList();

        versions.Count.ShouldBe(2);

        versions[0].GetProperty("amount").GetDecimal().ShouldBe(150.00m);
        versions[0].GetProperty("validityStart").GetString().ShouldBe("2026-01-01");
        versions[0].GetProperty("validityEnd").GetString().ShouldBe("2026-08-31");

        versions[1].GetProperty("amount").GetDecimal().ShouldBe(180.00m);
        versions[1].GetProperty("validityStart").GetString().ShouldBe("2026-09-01");
        versions[1].GetProperty("validityEnd").ValueKind.ShouldBe(JsonValueKind.Null);
        versions[1].GetProperty("changeReason").GetString().ShouldBe("tariff increase");
    }

    [Fact]
    public async Task Archive_Returns_204()
    {
        var caller = await NewAccount();
        var recurringExpenseId = await NewRecurringExpense(caller);

        var response = await DoPut(
            $"{RECURRING_EXPENSE}/{recurringExpenseId}/archive?archived=true", new { }, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unarchive_Returns_204()
    {
        var caller = await NewAccount();
        var recurringExpenseId = await NewRecurringExpense(caller);

        var archiveResponse = await DoPut(
            $"{RECURRING_EXPENSE}/{recurringExpenseId}/archive?archived=true", new { }, token: caller.Token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await DoPut(
            $"{RECURRING_EXPENSE}/{recurringExpenseId}/archive?archived=false", new { }, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Changing_The_Value_Of_Another_Users_Recurring_Expense_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var recurringExpenseId = await NewRecurringExpense(first);

        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(
            recurringExpenseId, new DateOnly(2026, 9, 1));

        var response = await DoPut(CHANGE_VALUE, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archiving_Another_Users_Recurring_Expense_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var recurringExpenseId = await NewRecurringExpense(first);

        var response = await DoPut(
            $"{RECURRING_EXPENSE}/{recurringExpenseId}/archive?archived=true", new { }, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Registering_Against_A_Category_Of_Another_User_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            second.PersonId, first.CategoryId, second.AccountId);

        var response = await DoPost(RECURRING_EXPENSE, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Name_Empty(string culture)
    {
        var caller = await NewAccount();

        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Name = string.Empty;

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.NAME_REQUIRED), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Amount_Not_Greater_Than_Zero(string culture)
    {
        var caller = await NewAccount();

        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Amount = 0;

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Due_Day_Out_Of_Range(string culture)
    {
        var caller = await NewAccount();

        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.DueDay = 32;

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.DAY_OUT_OF_RANGE), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Change_Reason_Empty(string culture)
    {
        var caller = await NewAccount();
        var recurringExpenseId = await NewRecurringExpense(caller);

        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(
            recurringExpenseId, new DateOnly(2026, 9, 1));
        request.ChangeReason = string.Empty;

        var response = await DoPut(CHANGE_VALUE, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.CHANGE_REASON_REQUIRED), culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Validity_Start_Not_Later_Than_The_Version_In_Effect(string culture)
    {
        var caller = await NewAccount();
        var recurringExpenseId = await NewRecurringExpense(caller, validityStart: new DateOnly(2026, 5, 1));

        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(
            recurringExpenseId, new DateOnly(2026, 5, 1));

        var response = await DoPut(CHANGE_VALUE, request, token: caller.Token, culture: culture);

        await ShouldCarrySingleError(response, nameof(ResourceErrorMessages.VALIDITY_START_MUST_BE_LATER), culture);
    }

    [Fact]
    public async Task Register_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var response = await DoPost(RECURRING_EXPENSE, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_Value_Without_Token_Is_Unauthorized()
    {
        var request = RequestChangeRecurringExpenseValueJsonBuilder.Build(Guid.NewGuid());

        var response = await DoPut(CHANGE_VALUE, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Archive_Without_Token_Is_Unauthorized()
    {
        var response = await DoPut($"{RECURRING_EXPENSE}/{Guid.NewGuid()}/archive?archived=true", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// GetAll is the only surface an archived bill's id stays reachable through: the monthly view
    /// excludes archived rows by design, so without this endpoint an archived bill could never be
    /// unarchived once its id fell out of every month the app had cached.
    /// </summary>
    [Fact]
    public async Task GetAll_Returns_Every_Recurring_Expense_Including_Archived_Ones()
    {
        var caller = await NewAccount();

        var activeId = await NewRecurringExpense(caller);
        var archivedId = await NewRecurringExpense(caller);

        var archiveResponse = await DoPut(
            $"{RECURRING_EXPENSE}/{archivedId}/archive?archived=true", new { }, token: caller.Token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await DoGet(RECURRING_EXPENSE, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var expenses = (await ReadJson(response))
            .GetProperty("recurringExpenses").EnumerateArray().ToList();

        expenses.Count.ShouldBe(2);

        expenses.Single(expense => expense.GetProperty("id").GetGuid() == archivedId)
            .GetProperty("archived").GetBoolean().ShouldBeTrue();

        expenses.Single(expense => expense.GetProperty("id").GetGuid() == activeId)
            .GetProperty("archived").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task GetAll_Only_Returns_The_Logged_Users_Recurring_Expenses()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        await NewRecurringExpense(first);

        var response = await DoGet(RECURRING_EXPENSE, token: second.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await ReadJson(response)).GetProperty("recurringExpenses").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet(RECURRING_EXPENSE);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

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

    private async Task<Guid> NewRecurringExpense(
        Caller caller, decimal amount = 150.00m, DateOnly? validityStart = null)
    {
        var request = RequestRegisterRecurringExpenseJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Amount = amount;
        request.ValidityStart = validityStart ?? new DateOnly(2026, 1, 1);

        var response = await DoPost(RECURRING_EXPENSE, request, token: caller.Token);
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
