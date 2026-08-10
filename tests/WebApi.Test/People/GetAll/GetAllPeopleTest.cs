using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.People.GetAll;

public class GetAllPeopleTest : BalanceClassFixture
{
    private const string METHOD = "api/person";
    private const string USER_METHOD = "api/user";

    public GetAllPeopleTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Error_Without_Token()
    {
        var response = await DoGet(METHOD);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registering_A_User_Creates_Exactly_One_Owner_Person()
    {
        var registerRequest = RequestRegisterUserJsonBuilder.Build();

        var token = await RegisterUserAndGetToken(registerRequest.Name, registerRequest.Email, registerRequest.Password);

        var response = await DoGet(METHOD, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var people = await ReadPeople(response);

        people.Count.ShouldBe(1);
        people[0].GetProperty("isAccountOwner").GetBoolean().ShouldBeTrue();
        people[0].GetProperty("name").GetString().ShouldBe(registerRequest.Name);
    }

    [Fact]
    public async Task Another_Accounts_People_Are_Not_Returned()
    {
        var firstAccount = RequestRegisterUserJsonBuilder.Build();
        var firstToken = await RegisterUserAndGetToken(firstAccount.Name, firstAccount.Email, firstAccount.Password);

        var spouse = RequestRegisterPersonJsonBuilder.Build();
        await DoPost(METHOD, spouse, token: firstToken);

        var secondAccount = RequestRegisterUserJsonBuilder.Build();
        var secondToken = await RegisterUserAndGetToken(secondAccount.Name, secondAccount.Email, secondAccount.Password);

        var response = await DoGet(METHOD, token: secondToken);

        var people = await ReadPeople(response);

        people.Count.ShouldBe(1);
        people.ShouldNotContain(person => person.GetProperty("name").GetString() == spouse.Name);
        people[0].GetProperty("name").GetString().ShouldBe(secondAccount.Name);
    }

    private async Task<string> RegisterUserAndGetToken(string name, string email, string password)
    {
        var request = RequestRegisterUserJsonBuilder.Build();
        request.Name = name;
        request.Email = email;
        request.Password = password;

        var response = await DoPost(USER_METHOD, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        return responseData.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<List<JsonElement>> ReadPeople(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var responseData = await JsonDocument.ParseAsync(body);

        return [.. responseData.RootElement.GetProperty("people").EnumerateArray()];
    }
}
