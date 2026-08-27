using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Creditors;

public class RegisterCreditorTest : BalanceClassFixture
{
    private const string CREDITOR = "api/Creditor";
    private const string USER = "api/user";

    public RegisterCreditorTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Register_Returns_201_With_The_Persisted_Shape()
    {
        var token = await NewAccountToken();

        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Name = "John Doe";
        request.Type = Balance.Communication.Enums.CreditorType.Person;

        var response = await DoPost(CREDITOR, request, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("name").GetString().ShouldBe("John Doe");
        body.GetProperty("type").GetInt32().ShouldBe((int)Balance.Communication.Enums.CreditorType.Person);
        body.GetProperty("archived").GetBoolean().ShouldBeFalse();
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Name_Empty(string culture)
    {
        var token = await NewAccountToken();

        var request = RequestRegisterCreditorJsonBuilder.Build();
        request.Name = string.Empty;

        var response = await DoPost(CREDITOR, request, token: token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.NAME_REQUIRED), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task Register_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterCreditorJsonBuilder.Build();

        var response = await DoPost(CREDITOR, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> NewAccountToken()
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJson(registerResponse)).GetProperty("token").GetString()!;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
