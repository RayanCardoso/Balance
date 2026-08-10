using Balance.Communication.Requests;
using Balance.Exception;
using CommonTestUtilities.Culture;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Login.DoLogin;

public class DoLoginTest : BalanceClassFixture
{
    private const string METHOD = "api/login";

    private readonly string _email;
    private readonly string _password;

    public DoLoginTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
        _email = webApplicationFactory.User_Team_Member.GetEmail();
        _password = webApplicationFactory.User_Team_Member.GetPassword();
    }

    [Fact]
    public async Task Success()
    {
        var request = new RequestLoginJson
        {
            Email = _email,
            Password = _password
        };

        var response = await DoPost(METHOD, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        responseData.RootElement.GetProperty("token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Wrong_Password(string culture)
    {
        var request = new RequestLoginJson
        {
            Email = _email,
            Password = "wrong-password"
        };

        var response = await DoPost(METHOD, request, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await AssertInvalidLoginMessage(response, culture);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Unknown_Email_Uses_The_Same_Message(string culture)
    {
        var request = new RequestLoginJson
        {
            Email = "nobody@example.com",
            Password = _password
        };

        var response = await DoPost(METHOD, request, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await AssertInvalidLoginMessage(response, culture);
    }

    private static async Task AssertInvalidLoginMessage(HttpResponseMessage response, string culture)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        var errors = responseData.RootElement.GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.EMAIL_OR_PASSWORD_INVALID), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }
}
