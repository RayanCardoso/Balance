using CommonTestUtilities.Requests;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Categories;

public class CategoryEndpointsTest : BalanceClassFixture
{
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public CategoryEndpointsTest(CustomWebApplicationFactory webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task Register_Category_Returns_Created()
    {
        var (token, _) = await NewAccount();
        var request = RequestRegisterCategoryJsonBuilder.Build();

        var response = await DoPost(CATEGORY, request, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("name").GetString().ShouldBe(request.Name);
        body.GetProperty("description").GetString().ShouldBe(request.Description);
        body.GetProperty("priority").GetInt32().ShouldBe((int)request.Priority);
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Register_Category_Without_Token_Is_Unauthorized()
    {
        var response = await DoPost(CATEGORY, RequestRegisterCategoryJsonBuilder.Build());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Categories_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet(CATEGORY);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Categories_Returns_Only_The_Callers_Categories()
    {
        var (firstToken, _) = await NewAccount();
        var registered = RequestRegisterCategoryJsonBuilder.Build();
        await DoPost(CATEGORY, registered, token: firstToken);

        var (secondToken, _) = await NewAccount();

        var firstList = await ReadJson(await DoGet(CATEGORY, token: firstToken));
        var secondList = await ReadJson(await DoGet(CATEGORY, token: secondToken));

        firstList.GetProperty("categories").EnumerateArray()
            .ShouldContain(category => category.GetProperty("name").GetString() == registered.Name);

        secondList.GetProperty("categories").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Register_Account_Returns_Created()
    {
        var (token, personId) = await NewAccount();
        var request = RequestRegisterAccountJsonBuilder.Build(personId);

        var response = await DoPost(ACCOUNT, request, token: token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("name").GetString().ShouldBe(request.Name);
        body.GetProperty("institution").GetString().ShouldBe(request.Institution);
        body.GetProperty("personId").GetGuid().ShouldBe(personId);
        body.GetProperty("closingDay").GetInt32().ShouldBe(request.ClosingDay!.Value);
        body.GetProperty("dueDay").GetInt32().ShouldBe(request.DueDay!.Value);
        body.GetProperty("limit").GetDecimal().ShouldBe(request.Limit!.Value);
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Register_Account_Without_Token_Is_Unauthorized()
    {
        var response = await DoPost(ACCOUNT, RequestRegisterAccountJsonBuilder.Build(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Accounts_Without_Token_Is_Unauthorized()
    {
        var response = await DoGet(ACCOUNT);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Accounts_Returns_Only_The_Callers_Accounts()
    {
        var (firstToken, firstPersonId) = await NewAccount();
        var registered = RequestRegisterAccountJsonBuilder.Build(firstPersonId);
        await DoPost(ACCOUNT, registered, token: firstToken);

        var (secondToken, _) = await NewAccount();

        var firstList = await ReadJson(await DoGet(ACCOUNT, token: firstToken));
        var secondList = await ReadJson(await DoGet(ACCOUNT, token: secondToken));

        firstList.GetProperty("accounts").EnumerateArray()
            .ShouldContain(account => account.GetProperty("name").GetString() == registered.Name);

        secondList.GetProperty("accounts").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Register_Account_With_A_Foreign_PersonId_Is_Not_Found()
    {
        var (_, firstPersonId) = await NewAccount();
        var (secondToken, _) = await NewAccount();

        var request = RequestRegisterAccountJsonBuilder.Build(firstPersonId);

        var response = await DoPost(ACCOUNT, request, token: secondToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Spec edge case: two categories of the same user carrying the same name are both accepted.
    /// Nothing enforces name uniqueness, and nothing should - "Mercado" for the month's shop and
    /// "Mercado" for the weekly market is a legitimate way to keep them apart by description.
    /// </summary>
    [Fact]
    public async Task Two_Categories_Of_The_Same_User_May_Share_A_Name()
    {
        var (token, _) = await NewAccount();

        var first = RequestRegisterCategoryJsonBuilder.Build();
        first.Name = "Mercado";

        var second = RequestRegisterCategoryJsonBuilder.Build();
        second.Name = "Mercado";

        (await DoPost(CATEGORY, first, token: token)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await DoPost(CATEGORY, second, token: token)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var listed = (await ReadJson(await DoGet(CATEGORY, token: token)))
            .GetProperty("categories").EnumerateArray()
            .Where(category => category.GetProperty("name").GetString() == "Mercado")
            .ToList();

        listed.Count.ShouldBe(2);
        listed[0].GetProperty("id").GetGuid().ShouldNotBe(listed[1].GetProperty("id").GetGuid());
    }

    /// <summary>Same edge case for accounts: two cards at the same bank may carry the same name.</summary>
    [Fact]
    public async Task Two_Accounts_Of_The_Same_Person_May_Share_A_Name()
    {
        var (token, personId) = await NewAccount();

        var first = RequestRegisterAccountJsonBuilder.Build(personId);
        first.Name = "Nubank Roxinho";

        var second = RequestRegisterAccountJsonBuilder.Build(personId);
        second.Name = "Nubank Roxinho";

        (await DoPost(ACCOUNT, first, token: token)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await DoPost(ACCOUNT, second, token: token)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var listed = (await ReadJson(await DoGet(ACCOUNT, token: token)))
            .GetProperty("accounts").EnumerateArray()
            .Where(account => account.GetProperty("name").GetString() == "Nubank Roxinho")
            .ToList();

        listed.Count.ShouldBe(2);
        listed[0].GetProperty("id").GetGuid().ShouldNotBe(listed[1].GetProperty("id").GetGuid());
    }

    private async Task<(string Token, Guid PersonId)> NewAccount()
    {
        var request = RequestRegisterUserJsonBuilder.Build();

        var registerResponse = await DoPost(USER, request);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var people = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().ToList();

        return (token, people[0].GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
