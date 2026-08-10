using Balance.Communication.Enums;
using Balance.Communication.Requests;
using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Incomes;

public class IncomeFlowTest : BalanceClassFixture
{
    private const string INCOME = "api/income";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public IncomeFlowTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Register_Without_Token_Is_Unauthorized()
    {
        var response = await DoPost(INCOME, RequestRegisterIncomeSourceJsonBuilder.Recurring(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_Payment_Without_Token_Is_Unauthorized()
    {
        var response = await DoPost($"{INCOME}/payment", RequestRegisterIncomePaymentJsonBuilder.Build(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_Value_Without_Token_Is_Unauthorized()
    {
        var response = await DoPut($"{INCOME}/value", RequestChangeIncomeSourceValueJsonBuilder.Build(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Monthly_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{INCOME}/2026/3");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Full_Flow_Register_Pay_And_Read_The_Month()
    {
        var (token, personId) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(personId);
        source.Amount = 5000m;
        source.ExpectedDay = 5;
        source.ValidityStart = new DateOnly(2026, 1, 1);

        var registerResponse = await DoPost(INCOME, source, token: token);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var incomeSourceId = (await ReadJson(registerResponse)).GetProperty("id").GetGuid();

        var payment = RequestRegisterIncomePaymentJsonBuilder.Build(incomeSourceId, new DateOnly(2026, 3, 1));
        payment.AmountReceived = 5000m;

        var paymentResponse = await DoPost($"{INCOME}/payment", payment, token: token);
        paymentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ReadJson(paymentResponse)).GetProperty("incomeSourceVersionId").GetGuid().ShouldNotBe(Guid.Empty);

        var monthResponse = await DoGet($"{INCOME}/2026/3", token: token);
        monthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var month = await ReadJson(monthResponse);
        var lines = month.GetProperty("lines").EnumerateArray().ToList();

        lines.Count.ShouldBe(1);
        lines[0].GetProperty("expectedAmount").GetDecimal().ShouldBe(5000m);
        lines[0].GetProperty("receivedAmount").GetDecimal().ShouldBe(5000m);
        lines[0].GetProperty("status").GetInt32().ShouldBe((int)IncomeStatus.Received);
        month.GetProperty("totalExpected").GetDecimal().ShouldBe(5000m);
        month.GetProperty("totalReceived").GetDecimal().ShouldBe(5000m);
    }

    [Fact]
    public async Task An_Unpaid_Month_Reports_Pending()
    {
        var (token, personId) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(personId);
        source.Amount = 2500m;
        source.ValidityStart = new DateOnly(2026, 1, 1);

        await DoPost(INCOME, source, token: token);

        var month = await ReadJson(await DoGet($"{INCOME}/2026/4", token: token));
        var line = month.GetProperty("lines").EnumerateArray().Single();

        line.GetProperty("receivedAmount").GetDecimal().ShouldBe(0m);
        line.GetProperty("status").GetInt32().ShouldBe((int)IncomeStatus.Pending);
    }

    [Fact]
    public async Task Changing_The_Value_Keeps_The_Old_Month_On_The_Old_Amount()
    {
        var (token, personId) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(personId);
        source.Amount = 3000m;
        source.ValidityStart = new DateOnly(2026, 1, 1);

        var incomeSourceId = (await ReadJson(await DoPost(INCOME, source, token: token)))
            .GetProperty("id").GetGuid();

        var change = RequestChangeIncomeSourceValueJsonBuilder.Build(incomeSourceId, new DateOnly(2026, 7, 1));
        change.Amount = 4500m;

        var changeResponse = await DoPut($"{INCOME}/value", change, token: token);
        changeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJson(changeResponse)).GetProperty("changeReason").GetString().ShouldBe(change.ChangeReason);

        var beforeRaise = await ReadJson(await DoGet($"{INCOME}/2026/3", token: token));
        var afterRaise = await ReadJson(await DoGet($"{INCOME}/2026/8", token: token));

        beforeRaise.GetProperty("lines").EnumerateArray().Single()
            .GetProperty("expectedAmount").GetDecimal().ShouldBe(3000m);

        afterRaise.GetProperty("lines").EnumerateArray().Single()
            .GetProperty("expectedAmount").GetDecimal().ShouldBe(4500m);
    }

    [Fact]
    public async Task Another_Account_Does_Not_See_The_Sources()
    {
        var (firstToken, firstPersonId) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(firstPersonId);
        source.ValidityStart = new DateOnly(2026, 1, 1);
        await DoPost(INCOME, source, token: firstToken);

        var (secondToken, _) = await NewAccount();

        var month = await ReadJson(await DoGet($"{INCOME}/2026/3", token: secondToken));

        month.GetProperty("lines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("totalExpected").GetDecimal().ShouldBe(0m);
    }

    [Fact]
    public async Task Registering_Against_Another_Accounts_Person_Is_Not_Found()
    {
        var (_, firstPersonId) = await NewAccount();
        var (secondToken, _) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(firstPersonId);

        var response = await DoPost(INCOME, source, token: secondToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Amount_Not_Greater_Than_Zero(string culture)
    {
        var (token, personId) = await NewAccount();

        var source = RequestRegisterIncomeSourceJsonBuilder.Recurring(personId);
        source.Amount = 0;

        var response = await DoPost(INCOME, source, token: token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task Error_Reference_Month_Invalid()
    {
        var (token, _) = await NewAccount();

        var response = await DoGet($"{INCOME}/2026/13", token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<(string Token, Guid PersonId)> NewAccount()
    {
        var request = RequestRegisterUserJsonBuilder.Build();

        var registerResponse = await DoPost(USER, request);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var people = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().ToList();

        return (token, people[0].GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
