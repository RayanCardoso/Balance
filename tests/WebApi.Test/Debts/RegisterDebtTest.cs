using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Debts;

public class RegisterDebtTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RegisterDebtTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// The use case builds installments 1..N in a single ascending for-loop and returns that same
    /// in-memory list - it never re-queries the database for the response - so there is no way to
    /// smuggle a pre-scrambled row set through this endpoint the way a defective `GetById().Include`
    /// could return one out of insertion/PostgreSQL order. What *can* still regress is the loop
    /// itself: a future change that built the schedule from an unordered structure (a dictionary, a
    /// parallel projection) would silently stop guaranteeing ascending `Number`. This test pins not
    /// just the count and the `Number` sequence but each installment's independently computed
    /// `ReferenceMonth`, so a shuffled result fails even though every individual value would still be
    /// "correct" in isolation.
    /// </summary>
    [Fact]
    public async Task Register_Returns_201_With_Ten_Installments_In_Ascending_Order()
    {
        var caller = await NewAccount();

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

        var installments = body.GetProperty("installments").EnumerateArray().ToList();

        installments.Count.ShouldBe(10);

        var expectedReferenceMonths = new[]
        {
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 12, 1),
            new DateOnly(2027, 1, 1)
        };

        for (var index = 0; index < installments.Count; index++)
        {
            installments[index].GetProperty("number").GetInt32().ShouldBe(index + 1);
            installments[index].GetProperty("expectedAmount").GetDecimal().ShouldBe(150.00m);

            var referenceMonth = DateOnly.Parse(installments[index].GetProperty("referenceMonth").GetString()!);
            referenceMonth.ShouldBe(expectedReferenceMonths[index]);
        }
    }

    [Fact]
    public async Task Error_Total_Less_Than_Principal()
    {
        var caller = await NewAccount();

        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = caller.CreditorId;
        request.PersonId = caller.PersonId;
        request.CategoryId = caller.CategoryId;
        request.PrincipalAmount = 1000.00m;
        request.TotalAmount = 500.00m;

        var response = await DoPost(DEBT, request, token: caller.Token, culture: "en");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        errors.ShouldHaveSingleItem().GetString().ShouldBe("The total amount must not be less than the principal.");
    }

    [Fact]
    public async Task Error_Open_Ended_With_Due_Day_Is_Not_Allowed()
    {
        var caller = await NewAccount();

        var request = RequestRegisterDebtJsonBuilder.BuildOpenEnded();
        request.CreditorId = caller.CreditorId;
        request.PersonId = caller.PersonId;
        request.CategoryId = caller.CategoryId;
        request.DueDay = 10;

        var response = await DoPost(DEBT, request, token: caller.Token, culture: "en");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        errors.ShouldHaveSingleItem().GetString()
            .ShouldBe("An open-ended debt must not have a due day or a number of installments.");
    }

    /// <summary>
    /// AD-004: a not-owned id is indistinguishable from a non-existent one. 404, never 403.
    /// </summary>
    [Fact]
    public async Task A_Creditor_Of_Another_Account_Is_Not_Found()
    {
        var first = await NewAccount();
        var second = await NewAccount();

        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();
        request.CreditorId = first.CreditorId;
        request.PersonId = second.PersonId;
        request.CategoryId = second.CategoryId;

        var response = await DoPost(DEBT, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterDebtJsonBuilder.BuildScheduled();

        var response = await DoPost(DEBT, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid CreditorId);

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

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
