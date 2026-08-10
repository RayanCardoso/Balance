using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Users.Register;

public class RegisterUserTest : BalanceClassFixture
{
    private const string METHOD = "api/user";

    public RegisterUserTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Success()
    {
        var request = RequestRegisterUserJsonBuilder.Build();

        var response = await DoPost(METHOD, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        responseData.RootElement.GetProperty("name").GetString().ShouldBe(request.Name);
        responseData.RootElement.GetProperty("token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Error_Name_Empty()
    {
        var request = RequestRegisterUserJsonBuilder.Build();
        request.Name = string.Empty;

        var response = await DoPost(METHOD, request, culture: "pt-BR");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        var errors = responseData.RootElement.GetProperty("errorMessages").EnumerateArray();

        errors.ShouldContain(error => error.GetString()!.Equals("O nome é obrigatório."));
    }
}
