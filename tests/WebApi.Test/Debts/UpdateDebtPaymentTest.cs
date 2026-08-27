using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace WebApi.Test.Debts;

public class UpdateDebtPaymentTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public UpdateDebtPaymentTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Correcting_A_Payment_Returns_200_With_The_Corrected_Values()
    {
        var caller = await NewAccount();
        var paymentId = await NewPayment(caller);

        var request = RequestUpdateDebtPaymentJsonBuilder.Build(
            amountPaid: 172.40m,
            paymentDate: new DateOnly(2026, 8, 15),
            accountId: null,
            type: CommunicationExpenseType.Debit);
        request.Notes = "corrected after the bill was re-read";

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldBe(paymentId);
        body.GetProperty("amountPaid").GetDecimal().ShouldBe(172.40m);
        body.GetProperty("paymentDate").GetString().ShouldBe("2026-08-15");
        body.GetProperty("notes").GetString().ShouldBe("corrected after the bill was re-read");
        body.GetProperty("type").GetInt32().ShouldBe((int)CommunicationExpenseType.Debit);
        body.GetProperty("accountId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>
    /// FINDING 2: UpdateDebtPaymentUseCase used to assign request.AccountId with no ownership check,
    /// so a caller's own payment id combined with another account's accountId would satisfy the
    /// foreign key and persist silently. It must now 404, exactly like registering a new payment does.
    /// </summary>
    [Fact]
    public async Task Correcting_With_Another_Accounts_Id_Returns_404_ACCOUNT_NOT_FOUND()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var paymentId = await NewPayment(first);

        var secondsAccountResponse = await DoPost(
            ACCOUNT, RequestRegisterAccountJsonBuilder.Build(second.PersonId), token: second.Token);
        secondsAccountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondsAccountId = (await ReadJson(secondsAccountResponse)).GetProperty("id").GetGuid();

        var request = RequestUpdateDebtPaymentJsonBuilder.Build(
            amountPaid: 172.40m, accountId: secondsAccountId);

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: first.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// AD-004: a not-owned id is indistinguishable from a non-existent one. 404, never 403, never 200.
    /// </summary>
    [Fact]
    public async Task Another_Accounts_Payment_Id_Returns_404_On_Update()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var paymentId = await NewPayment(first);

        var request = RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: 172.40m);

        var response = await DoPut($"{PAYMENT}/{paymentId}", request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Payment_Without_Token_Is_Unauthorized()
    {
        var request = RequestUpdateDebtPaymentJsonBuilder.Build(amountPaid: 172.40m);

        var response = await DoPut($"{PAYMENT}/{Guid.NewGuid()}", request);

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

        return new RegisteredDebt(body.GetProperty("id").GetGuid(), firstInstallment.GetProperty("id").GetGuid());
    }

    private async Task<Guid> NewPayment(Caller caller)
    {
        var debt = await NewDebt(caller);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null,
            type: CommunicationExpenseType.Pix);
        request.AmountPaid = 150.00m;

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
