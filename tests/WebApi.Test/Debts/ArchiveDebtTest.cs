using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Debts;

public class ArchiveDebtTest : BalanceClassFixture
{
    private const string DEBT = "api/Debt";
    private const string CREDITOR = "api/Creditor";
    private const string CATEGORY = "api/category";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public ArchiveDebtTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    /// <summary>
    /// Asserts the round trip, not just the 204: archiving must actually remove the debt from the
    /// default list, otherwise a controller that returns NoContent without truly persisting the
    /// flag would still pass a status-code-only check.
    /// </summary>
    [Fact]
    public async Task Archiving_Then_Listing_Omits_It_From_The_Default_List()
    {
        var caller = await NewAccount();

        var active = await NewDebt(caller);
        var archived = await NewDebt(caller);

        var archiveResponse = await DoPut($"{DEBT}/{archived.Id}/archive?archived=true", new { }, token: caller.Token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var defaultResponse = await DoGet(DEBT, token: caller.Token);
        defaultResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var defaultIds = (await ReadJson(defaultResponse)).GetProperty("debts").EnumerateArray()
            .Select(debt => debt.GetProperty("id").GetGuid())
            .ToList();

        defaultIds.ShouldContain(active.Id);
        defaultIds.ShouldNotContain(archived.Id);

        var includeInactiveResponse = await DoGet($"{DEBT}?includeInactive=true", token: caller.Token);
        includeInactiveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allDebts = (await ReadJson(includeInactiveResponse)).GetProperty("debts").EnumerateArray().ToList();

        allDebts.Single(debt => debt.GetProperty("id").GetGuid() == archived.Id)
            .GetProperty("archived").GetBoolean().ShouldBeTrue();
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

        var response = await DoPut($"{DEBT}/{debt.Id}/archive?archived=true", new { }, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_Without_Token_Is_Unauthorized()
    {
        var response = await DoPut($"{DEBT}/{Guid.NewGuid()}/archive?archived=true", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid CreditorId);

    private sealed record RegisteredDebt(Guid Id);

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
        request.PrincipalAmount = 500.00m;
        request.TotalAmount = 500.00m;
        request.StartDate = new DateOnly(2026, 3, 20);
        request.DueDay = 10;
        request.InstallmentCount = 5;

        var response = await DoPost(DEBT, request, token: caller.Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        return new RegisteredDebt(body.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
