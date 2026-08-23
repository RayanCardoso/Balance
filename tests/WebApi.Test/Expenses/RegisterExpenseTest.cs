using Balance.Communication.Enums;
using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Expenses;

public class RegisterExpenseTest : BalanceClassFixture
{
    private const string EXPENSE = "api/expense";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RegisterExpenseTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Credit_Expense_Past_The_Closing_Day_Is_Created_In_The_Following_Competence_Month()
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 21);

        var response = await DoPost(EXPENSE, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("name").GetString().ShouldBe(request.Name);
        body.GetProperty("amount").GetDecimal().ShouldBe(request.Amount);
        body.GetProperty("type").GetInt32().ShouldBe((int)ExpenseType.Credit);
        body.GetProperty("personId").GetGuid().ShouldBe(caller.PersonId);
        body.GetProperty("categoryId").GetGuid().ShouldBe(caller.CategoryId);
        body.GetProperty("accountId").GetGuid().ShouldBe(caller.AccountId);
        body.GetProperty("date").GetString().ShouldBe("2026-08-21");
        body.GetProperty("competenceMonth").GetString().ShouldBe("2026-09-01");
    }

    [Fact]
    public async Task Credit_Expense_On_The_Closing_Day_Stays_In_The_Month_Of_The_Date()
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Type = ExpenseType.Credit;
        request.Date = new DateOnly(2026, 8, 20);

        var response = await DoPost(EXPENSE, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ReadJson(response)).GetProperty("competenceMonth").GetString().ShouldBe("2026-08-01");
    }

    [Fact]
    public async Task An_Account_Of_Another_Person_Of_The_Same_User_Is_Accepted()
    {
        var caller = await NewAccount(closingDay: 20);
        var otherPersonId = await NewPerson(caller.Token);

        // The expense belongs to the second person; the card belongs to the first.
        var request = RequestRegisterExpenseJsonBuilder.Build(otherPersonId, caller.CategoryId, caller.AccountId);

        var response = await DoPost(EXPENSE, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("personId").GetGuid().ShouldBe(otherPersonId);
        body.GetProperty("accountId").GetGuid().ShouldBe(caller.AccountId);
    }

    [Fact]
    public async Task A_Category_Of_Another_User_Is_Not_Found()
    {
        var first = await NewAccount(closingDay: 20);
        var second = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(second.PersonId, first.CategoryId, second.AccountId);

        var response = await DoPost(EXPENSE, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_Account_Of_Another_User_Is_Not_Found()
    {
        var first = await NewAccount(closingDay: 20);
        var second = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(second.PersonId, second.CategoryId, first.AccountId);

        var response = await DoPost(EXPENSE, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Amount_Not_Greater_Than_Zero(string culture)
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Amount = 0;

        var response = await DoPost(EXPENSE, request, token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Name_Empty(string culture)
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Name = string.Empty;

        var response = await DoPost(EXPENSE, request, token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.NAME_REQUIRED), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Credit_Without_Account(string culture)
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterExpenseJsonBuilder.Build(caller.PersonId, caller.CategoryId, caller.AccountId);
        request.Type = ExpenseType.Credit;
        request.AccountId = null;

        var response = await DoPost(EXPENSE, request, token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.ACCOUNT_REQUIRED_FOR_CREDIT), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task Register_Expense_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterExpenseJsonBuilder.Build(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var response = await DoPost(EXPENSE, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

    private async Task<Caller> NewAccount(int? closingDay)
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var people = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().ToList();

        var personId = people[0].GetProperty("id").GetGuid();

        var categoryResponse = await DoPost(CATEGORY, RequestRegisterCategoryJsonBuilder.Build(), token: token);
        categoryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var accountRequest = RequestRegisterAccountJsonBuilder.Build(personId);
        accountRequest.ClosingDay = closingDay;

        var accountResponse = await DoPost(ACCOUNT, accountRequest, token: token);
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return new Caller(
            token,
            personId,
            (await ReadJson(categoryResponse)).GetProperty("id").GetGuid(),
            (await ReadJson(accountResponse)).GetProperty("id").GetGuid());
    }

    private async Task<Guid> NewPerson(string token)
    {
        var response = await DoPost(PERSON, RequestRegisterPersonJsonBuilder.Build(), token: token);

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
