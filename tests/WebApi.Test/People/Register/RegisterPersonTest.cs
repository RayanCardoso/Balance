using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.People.Register;

public class RegisterPersonTest : BalanceClassFixture
{
    private const string METHOD = "api/person";

    private readonly string _token;

    public RegisterPersonTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
        _token = webApplicationFactory.User_Team_Member.GetToken();
    }

    [Fact]
    public async Task Success()
    {
        var request = RequestRegisterPersonJsonBuilder.Build();

        var response = await DoPost(METHOD, request, token: _token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        responseData.RootElement.GetProperty("name").GetString().ShouldBe(request.Name);
        responseData.RootElement.GetProperty("description").GetString().ShouldBe(request.Description);
        responseData.RootElement.GetProperty("isAccountOwner").GetBoolean().ShouldBeFalse();
        responseData.RootElement.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Error_Without_Token()
    {
        var request = RequestRegisterPersonJsonBuilder.Build();

        var response = await DoPost(METHOD, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Name_Empty(string culture)
    {
        var request = RequestRegisterPersonJsonBuilder.Build();
        request.Name = string.Empty;

        var response = await DoPost(METHOD, request, token: _token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        var errors = responseData.RootElement.GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.NAME_REQUIRED), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }
}
