using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Debts;

public class GetMonthlyDebtTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string PAYMENT = "api/Debt/payment";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public GetMonthlyDebtTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// A ten-installment debt whose schedule starts in January 2026 (StartDate on or before its own
    /// due day, so the first competence month is the start month itself - see
    /// DebtScheduleBuilder.FirstCompetenceMonth), paid only for the first installment. Month 1 must
    /// read the paid installment as Paid; month 2 must read the second, unpaid installment as
    /// Pending, and since nothing was paid that month TotalCommitted falls back to what was
    /// expected.
    /// </summary>
    [Fact]
    public async Task Month_1_Reads_Paid_And_Month_2_Reads_Pending_With_Committed_Equal_To_Expected()
    {
        var caller = await NewAccount();
        var debt = await NewScheduledDebt(caller);

        var paymentRequest = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: null);
        paymentRequest.AmountPaid = 150.00m;
        paymentRequest.PaymentDate = new DateOnly(2026, 1, 10);

        var paymentResponse = await DoPost(PAYMENT, paymentRequest, token: caller.Token);
        paymentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var month1 = await GetMonth(caller, 2026, 1);
        var month1Line = month1.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();

        month1Line.GetProperty("status").GetInt32().ShouldBe((int)Balance.Communication.Enums.ExpenseStatus.Paid);
        month1.GetProperty("totalPaid").GetDecimal().ShouldBe(150.00m);
        month1.GetProperty("totalExpected").GetDecimal().ShouldBe(150.00m);
        month1.GetProperty("totalCommitted").GetDecimal().ShouldBe(150.00m);

        var month2 = await GetMonth(caller, 2026, 2);
        var month2Line = month2.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();

        month2Line.GetProperty("status").GetInt32().ShouldBe((int)Balance.Communication.Enums.ExpenseStatus.Pending);
        month2.GetProperty("totalPaid").GetDecimal().ShouldBe(0.00m);
        month2.GetProperty("totalExpected").GetDecimal().ShouldBe(150.00m);

        // Nothing paid in month 2, so committed falls back to the expected amount.
        month2.GetProperty("totalCommitted").GetDecimal().ShouldBe(150.00m);
    }

    /// <summary>
    /// FINDING 3 (DVEW-01 AC1): the monthly line must carry the payment's type and account name.
    /// This is the real query end to end - the only level that could have caught GetForMonth's
    /// missing ThenInclude on the payment's Account (FINDING 1).
    /// </summary>
    [Fact]
    public async Task A_Paid_Installments_Line_Reports_The_Payment_Type_And_Account_Name()
    {
        var caller = await NewAccount();
        var debt = await NewScheduledDebt(caller);

        var accountResponse = await DoPost(
            ACCOUNT, RequestRegisterAccountJsonBuilder.Build(caller.PersonId), token: caller.Token);
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var accountBody = await ReadJson(accountResponse);
        var accountId = accountBody.GetProperty("id").GetGuid();
        var accountName = accountBody.GetProperty("name").GetString();

        var paymentRequest = RequestRegisterDebtPaymentJsonBuilder.Build(
            debt.Id, debtInstallmentId: debt.FirstInstallmentId, accountId: accountId,
            type: Balance.Communication.Enums.ExpenseType.Credit);
        paymentRequest.AmountPaid = 150.00m;
        paymentRequest.PaymentDate = new DateOnly(2026, 1, 10);

        var paymentResponse = await DoPost(PAYMENT, paymentRequest, token: caller.Token);
        paymentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var month = await GetMonth(caller, 2026, 1);
        var line = month.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();

        line.GetProperty("type").GetInt32().ShouldBe((int)Balance.Communication.Enums.ExpenseType.Credit);
        line.GetProperty("accountId").GetGuid().ShouldBe(accountId);
        line.GetProperty("accountName").GetString().ShouldBe(accountName);
    }

    /// <summary>
    /// Assert every error message as a hard-coded literal with the culture pinned explicitly (L-010)
    /// - never read back from ResourceErrorMessages or its ResourceManager.
    /// </summary>
    [Fact]
    public async Task An_Invalid_Month_Returns_400_With_The_PtBr_Message()
    {
        var caller = await NewAccount();

        var response = await DoGet($"{DEBT}/2026/13", token: caller.Token, culture: "pt-BR");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        errors.ShouldHaveSingleItem().GetString().ShouldBe("O mês de referência é inválido.");
    }

    [Fact]
    public async Task An_Empty_Month_Returns_200_With_Zeroed_Totals()
    {
        var caller = await NewAccount();

        var month = await GetMonth(caller, 2026, 8);

        month.GetProperty("lines").EnumerateArray().ShouldBeEmpty();
        month.GetProperty("totalExpected").GetDecimal().ShouldBe(0.00m);
        month.GetProperty("totalPaid").GetDecimal().ShouldBe(0.00m);
        month.GetProperty("totalCommitted").GetDecimal().ShouldBe(0.00m);
    }

    [Fact]
    public async Task Without_Token_Is_Unauthorized()
    {
        var response = await DoGet($"{DEBT}/2026/8");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// A route-collision sentinel: GET api/Debt/{id:guid} must keep working alongside the new
    /// GET api/Debt/{year:int}/{month:int} route. A collision between the two surfaces only as an
    /// unexpected 404 or 400 at test time, never as a build error, so this is the only detector.
    /// </summary>
    [Fact]
    public async Task GetById_Still_Works_Alongside_The_Monthly_Route()
    {
        var caller = await NewAccount();
        var debt = await NewScheduledDebt(caller);

        var response = await DoGet($"{DEBT}/{debt.Id}", token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldBe(debt.Id);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid CreditorId);

    private sealed record RegisteredDebt(Guid Id, Guid FirstInstallmentId);

    private async Task<JsonElement> GetMonth(Caller caller, int year, int month)
    {
        var response = await DoGet($"{DEBT}/{year}/{month}", token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await ReadJson(response);
    }

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

    private async Task<RegisteredDebt> NewScheduledDebt(Caller caller)
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = caller.CreditorId;
        request.PersonId = caller.PersonId;
        request.CategoryId = caller.CategoryId;
        request.PrincipalAmount = 1500.00m;
        request.TotalAmount = 1500.00m;
        // On or before the due day, so the schedule's first competence month is January itself.
        request.StartDate = new DateOnly(2026, 1, 5);
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
