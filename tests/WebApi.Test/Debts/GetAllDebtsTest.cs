using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace WebApi.Test.Debts;

public class GetAllDebtsTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public GetAllDebtsTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task GetAll_Honours_The_CreditorId_Filter()
    {
        var caller = await NewAccount();

        var otherCreditorResponse = await DoPost(CREDITOR, RequestRegisterCreditorJsonBuilder.Build(), token: caller.Token);
        otherCreditorResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var otherCreditorId = (await ReadJson(otherCreditorResponse)).GetProperty("id").GetGuid();

        var matching = await NewDebt(caller, caller.CreditorId, caller.PersonId, principal: 100m);
        await NewDebt(caller, otherCreditorId, caller.PersonId, principal: 200m);

        var response = await DoGet($"{DEBT}?creditorId={caller.CreditorId}", token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ids = (await ReadJson(response)).GetProperty("debts").EnumerateArray()
            .Select(debt => debt.GetProperty("id").GetGuid())
            .ToList();

        ids.ShouldBe([matching.Id]);
    }

    [Fact]
    public async Task GetAll_Honours_The_PersonId_Filter()
    {
        var caller = await NewAccount();

        var otherPersonResponse = await DoPost(PERSON, RequestRegisterPersonJsonBuilder.Build(), token: caller.Token);
        otherPersonResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var otherPersonId = (await ReadJson(otherPersonResponse)).GetProperty("id").GetGuid();

        var matching = await NewDebt(caller, caller.CreditorId, caller.PersonId, principal: 100m);
        await NewDebt(caller, caller.CreditorId, otherPersonId, principal: 200m);

        var response = await DoGet($"{DEBT}?personId={caller.PersonId}", token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ids = (await ReadJson(response)).GetProperty("debts").EnumerateArray()
            .Select(debt => debt.GetProperty("id").GetGuid())
            .ToList();

        ids.ShouldBe([matching.Id]);
    }

    /// <summary>
    /// Settled is derived from payments, not stored, and is filtered out by default the same way
    /// an archived debt is - includeInactive brings both back.
    /// </summary>
    [Fact]
    public async Task A_Settled_Debt_Is_Absent_By_Default_And_Present_With_IncludeInactive()
    {
        var caller = await NewAccount();

        var settled = await NewDebt(caller, caller.CreditorId, caller.PersonId, principal: 100m, installmentCount: 1);

        var payRequest = RequestRegisterDebtPaymentJsonBuilder.Build(
            settled.Id, debtInstallmentId: settled.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Pix);
        payRequest.AmountPaid = 100.00m;
        payRequest.PaymentDate = new DateOnly(2026, 4, 10);

        var payResponse = await DoPost(PAYMENT, payRequest, token: caller.Token);
        payResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var defaultResponse = await DoGet(DEBT, token: caller.Token);
        defaultResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var defaultIds = (await ReadJson(defaultResponse)).GetProperty("debts").EnumerateArray()
            .Select(debt => debt.GetProperty("id").GetGuid())
            .ToList();

        defaultIds.ShouldNotContain(settled.Id);

        var includeInactiveResponse = await DoGet($"{DEBT}?includeInactive=true", token: caller.Token);
        includeInactiveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allDebts = (await ReadJson(includeInactiveResponse)).GetProperty("debts").EnumerateArray().ToList();

        var settledLine = allDebts.Single(debt => debt.GetProperty("id").GetGuid() == settled.Id);
        settledLine.GetProperty("isSettled").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// AD-004: the list route is scoped by the logged user first, so a foreign creditorId used as
    /// a filter can only ever narrow the caller's own debts - it never surfaces another account's
    /// data, and it never errors either, it simply yields nothing.
    /// </summary>
    [Fact]
    public async Task Another_Accounts_Debts_Never_Appear_In_The_List()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var firstDebt = await NewDebt(first, first.CreditorId, first.PersonId, principal: 100m);

        var response = await DoGet($"{DEBT}?creditorId={first.CreditorId}", token: second.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ids = (await ReadJson(response)).GetProperty("debts").EnumerateArray()
            .Select(debt => debt.GetProperty("id").GetGuid())
            .ToList();

        ids.ShouldNotContain(firstDebt.Id);
        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet(DEBT);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid CreditorId);

    private sealed record RegisteredDebt(Guid Id, Guid FirstInstallmentId);

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

        var creditorResponse = await DoPost(CREDITOR, RequestRegisterCreditorJsonBuilder.Build(), token: token);
        creditorResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return new Caller(
            token,
            personId,
            (await ReadJson(categoryResponse)).GetProperty("id").GetGuid(),
            (await ReadJson(creditorResponse)).GetProperty("id").GetGuid());
    }

    private async Task<RegisteredDebt> NewDebt(
        Caller caller, Guid creditorId, Guid personId, decimal principal, int installmentCount = 10)
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = creditorId;
        request.PersonId = personId;
        request.CategoryId = caller.CategoryId;
        request.PrincipalAmount = principal;
        request.TotalAmount = principal;
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 10;
        request.InstallmentCount = installmentCount;

        var response = await DoPost(DEBT, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);
        var firstInstallment = body.GetProperty("installments").EnumerateArray().First();

        return new RegisteredDebt(
            body.GetProperty("id").GetGuid(),
            firstInstallment.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
