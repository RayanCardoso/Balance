using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Creditors;

public class ArchiveCreditorTest : BalanceClassFixture
{
    private const string CREDITOR = "api/Creditor";
    private const string USER = "api/user";

    public ArchiveCreditorTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Archive_Returns_204()
    {
        var token = await NewAccountToken();
        var creditorId = await NewCreditor(token);

        var response = await DoPut($"{CREDITOR}/{creditorId}/archive?archived=true", new { }, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unarchive_Returns_204()
    {
        var token = await NewAccountToken();
        var creditorId = await NewCreditor(token);

        var archiveResponse = await DoPut($"{CREDITOR}/{creditorId}/archive?archived=true", new { }, token: token);
        archiveResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await DoPut($"{CREDITOR}/{creditorId}/archive?archived=false", new { }, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// AD-004: a not-owned id is indistinguishable from a non-existent one, so ids cannot be
    /// probed across accounts. 404, never 403 or 204.
    /// </summary>
    [Fact]
    public async Task Archiving_Another_Accounts_Creditor_Is_Not_Found()
    {
        var first = await NewAccountToken();
        var second = await NewAccountToken();

        var creditorId = await NewCreditor(first);

        var response = await DoPut($"{CREDITOR}/{creditorId}/archive?archived=true", new { }, token: second);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_Without_Token_Is_Unauthorized()
    {
        var response = await DoPut($"{CREDITOR}/{Guid.NewGuid()}/archive?archived=true", new { });

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
