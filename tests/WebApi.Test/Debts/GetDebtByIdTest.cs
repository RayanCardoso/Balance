using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Debts;

public class GetDebtByIdTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public GetDebtByIdTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task GetById_Returns_200_With_Computed_Balance_And_Settled_Flag()
    {
        var caller = await NewAccount();
        var debt = await NewDebt(caller);

        var response = await DoGet($"{DEBT}/{debt.Id}", token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("outstandingBalance").GetDecimal().ShouldBe(1500.00m);
        body.GetProperty("isSettled").GetBoolean().ShouldBeFalse();
    }

    /// <summary>
    /// THE ROUND TRIP: register a scheduled debt, pay one installment, then GET the debt and
    /// assert the outstanding balance dropped by EXACTLY the amount paid. This is the first point
    /// in the whole feature where the derived balance can be proven end to end through real HTTP
    /// and a real query - OutstandingBalance is TotalAmount minus the sum of recorded payments,
    /// never a stored, cached figure.
    /// </summary>
    [Fact]
    public async Task Paying_One_Installment_Drops_Outstanding_Balance_By_Exactly_The_Amount_Paid()
    {
        var caller = await NewAccount();
        var debt = await NewDebt(caller);

        var paymentRequest = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null,
            type: Balance.Communication.Enums.ExpenseType.Pix);
        paymentRequest.AmountPaid = 150.00m;
        paymentRequest.PaymentDate = new DateOnly(2026, 4, 10);

        var paymentResponse = await DoPost(PAYMENT, paymentRequest, token: caller.Token);
        paymentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await DoGet($"{DEBT}/{debt.Id}", token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("outstandingBalance").GetDecimal().ShouldBe(1350.00m);
        body.GetProperty("isSettled").GetBoolean().ShouldBeFalse();
    }

    /// <summary>
    /// AD-004: a not-owned id is indistinguishable from a non-existent one. 404, never 403.
    /// </summary>
    [Fact]
    public async Task A_Debt_Of_Another_Account_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var debt = await NewDebt(first);

        var response = await DoGet($"{DEBT}/{debt.Id}", token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{DEBT}/{Guid.NewGuid()}");

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

    private async Task<RegisteredDebt> NewDebt(Caller caller)
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = caller.CreditorId;
        request.PersonId = caller.PersonId;
        request.CategoryId = caller.CategoryId;
        request.PrincipalAmount = 1500.00m;
        request.TotalAmount = 1500.00m;
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 10;
        request.InstallmentCount = 10;

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
