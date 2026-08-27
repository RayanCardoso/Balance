using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace WebApi.Test.Creditors;

public class GetCreditorSummaryTest : BalanceClassFixture
{
    private const string CREDITOR = "api/Creditor";
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public GetCreditorSummaryTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// Two debts against one creditor with different amounts, one settled - including the settled
    /// one in the sum by mistake would land on a visibly different number, because it is left
    /// overpaid (a negative remainder) rather than merely paid off at zero.
    /// </summary>
    [Fact]
    public async Task OutstandingBalance_Equals_The_Sum_Of_The_Unsettled_Debts_Remainders()
    {
        var caller = await NewAccount();

        await NewDebt(caller, principal: 300.00m);

        var settled = await NewDebt(caller, principal: 100.00m);
        var overpayment = RequestRegisterDebtPaymentJsonBuilder.Build(
            settled.Id, debtInstallmentId: settled.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Pix);
        overpayment.AmountPaid = 150.00m;
        overpayment.PaymentDate = new DateOnly(2026, 4, 10);

        var payResponse = await DoPost(PAYMENT, overpayment, token: caller.Token);
        payResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await DoGet($"{CREDITOR}/{caller.CreditorId}/summary", token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("unsettledDebtCount").GetInt32().ShouldBe(1);
        body.GetProperty("totalOwed").GetDecimal().ShouldBe(300.00m);
        body.GetProperty("totalPaid").GetDecimal().ShouldBe(0m);
        body.GetProperty("outstandingBalance").GetDecimal().ShouldBe(300.00m);
    }

    /// <summary>
    /// AD-004: a not-owned id is indistinguishable from a non-existent one. 404, never 403.
    /// </summary>
    [Fact]
    public async Task A_Creditor_Of_Another_Account_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var response = await DoGet($"{CREDITOR}/{first.CreditorId}/summary", token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{CREDITOR}/{Guid.NewGuid()}/summary");

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

    private async Task<RegisteredDebt> NewDebt(Caller caller, decimal principal)
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = caller.CreditorId;
        request.PersonId = caller.PersonId;
        request.CategoryId = caller.CategoryId;
        request.PrincipalAmount = principal;
        request.TotalAmount = principal;
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 10;
        request.InstallmentCount = 1;

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
