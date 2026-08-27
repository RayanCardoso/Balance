using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace WebApi.Test.Debts;

public class RegisterDebtPaymentTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RegisterDebtPaymentTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// A pix or cash payment legitimately has no registered account. Rejecting it here would be a
    /// real usability defect, so this is the case that proves the endpoint accepts it.
    /// </summary>
    [Fact]
    public async Task Paying_By_Pix_With_No_Account_Returns_201_With_Amount_Date_And_ReferenceMonth()
    {
        var caller = await NewAccount();
        var debt = await NewDebt(caller);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Pix);
        request.AmountPaid = 150.00m;
        request.PaymentDate = new DateOnly(2026, 8, 10);

        var response = await DoPost(PAYMENT, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("amountPaid").GetDecimal().ShouldBe(150.00m);
        body.GetProperty("paymentDate").GetString().ShouldBe("2026-08-10");
        body.GetProperty("accountId").ValueKind.ShouldBe(JsonValueKind.Null);

        var referenceMonth = DateOnly.Parse(body.GetProperty("referenceMonth").GetString()!);
        referenceMonth.ShouldBe(debt.FirstInstallmentReferenceMonth);
    }

    [Fact]
    public async Task Paying_By_Credit_With_No_Account_Returns_400_Account_Required_For_Credit()
    {
        var caller = await NewAccount();
        var debt = await NewDebt(caller);

        var request = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Credit);

        var response = await DoPost(PAYMENT, request, token: caller.Token, culture: "en");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        errors.ShouldHaveSingleItem().GetString().ShouldBe("An account is required for a credit expense.");
    }

    /// <summary>
    /// The EF in-memory provider used by these tests ignores unique indexes, so this passing proves
    /// the use case's own probe (GetByInstallment) is doing the work, not the database constraint.
    /// </summary>
    [Fact]
    public async Task A_Second_Payment_On_The_Same_Installment_Returns_400_Payment_Already_Recorded()
    {
        var caller = await NewAccount();
        var debt = await NewDebt(caller);

        var first = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Pix);

        var firstResponse = await DoPost(PAYMENT, first, token: caller.Token);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null, type: CommunicationExpenseType.Pix);

        var response = await DoPost(PAYMENT, second, token: caller.Token, culture: "en");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        errors.ShouldHaveSingleItem().GetString().ShouldBe("A payment has already been recorded for this reference month.");
    }

    [Fact]
    public async Task Register_Payment_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterDebtPaymentJsonBuilder.Build(Guid.NewGuid());

        var response = await DoPost(PAYMENT, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid CreditorId);

    private sealed record RegisteredDebt(Guid Id, Guid FirstInstallmentId, DateOnly FirstInstallmentReferenceMonth);

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
            firstInstallment.GetProperty("id").GetGuid(),
            DateOnly.Parse(firstInstallment.GetProperty("referenceMonth").GetString()!));
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
