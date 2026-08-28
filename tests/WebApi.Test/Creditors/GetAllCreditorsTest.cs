using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Creditors;

public class GetAllCreditorsTest : BalanceClassFixture
{
    private const string CREDITOR = "api/Creditor";
    private const string USER = "api/user";

    public GetAllCreditorsTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task GetAll_Only_Returns_The_Logged_Users_Creditors()
    {
        var first = await NewAccountToken();
        var second = await NewAccountToken();

        await NewCreditor(first);

        var response = await DoGet(CREDITOR, token: second);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await ReadJson(response)).GetProperty("creditors").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Archiving_Then_Listing_Omits_It_By_Default_And_Includes_It_When_Requested()
    {
        var token = await NewAccountToken();

        var activeId = await NewCreditor(token);
        var archivedId = await NewCreditor(token);

        var archiveResponse = await DoPut(
            $"{CREDITOR}/{archivedId}/archive?archived=true", new { }, token: token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var defaultResponse = await DoGet(CREDITOR, token: token);
        defaultResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var defaultIds = (await ReadJson(defaultResponse))
            .GetProperty("creditors").EnumerateArray()
            .Select(creditor => creditor.GetProperty("id").GetGuid())
            .ToList();

        defaultIds.ShouldContain(activeId);
        defaultIds.ShouldNotContain(archivedId);

        var includeArchivedResponse = await DoGet($"{CREDITOR}?includeArchived=true", token: token);
        includeArchivedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allCreditors = (await ReadJson(includeArchivedResponse))
            .GetProperty("creditors").EnumerateArray().ToList();

        allCreditors.Single(creditor => creditor.GetProperty("id").GetGuid() == archivedId)
            .GetProperty("archived").GetBoolean().ShouldBeTrue();

        allCreditors.Single(creditor => creditor.GetProperty("id").GetGuid() == activeId)
            .GetProperty("archived").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task GetAll_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet(CREDITOR);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> NewAccountToken()
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(registerResponse)).GetProperty("token").GetString()!;
    }

    private async Task<Guid> NewCreditor(string token)
    {
        var request = RequestRegisterCreditorJsonBuilder.Build();

        var response = await DoPost(CREDITOR, request, token: token);
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
