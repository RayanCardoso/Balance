using Balance.Communication.Enums;
using Balance.Exception;
using CommonTestUtilities.Culture;
using CommonTestUtilities.Requests;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace WebApi.Test.Expenses;

public class RegisterInstallmentPlanTest : BalanceClassFixture
{
    private const string INSTALLMENT_PLAN = "api/expense/installment-plan";
    private const string CATEGORY = "api/category";
    private const string ACCOUNT = "api/account";
    private const string PERSON = "api/person";
    private const string USER = "api/user";

    public RegisterInstallmentPlanTest(CustomWebApplicationFactory webApplicationFactory)
        : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task A_Plan_Of_100_Over_3_Returns_Three_Installments_In_Consecutive_Competence_Months()
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.TotalAmount = 100.00m;
        request.InstallmentCount = 3;
        request.StartDate = new DateOnly(2026, 8, 21);

        var response = await DoPost(INSTALLMENT_PLAN, request, token: caller.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJson(response);

        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("totalAmount").GetDecimal().ShouldBe(100.00m);
        body.GetProperty("installmentCount").GetInt32().ShouldBe(3);
        body.GetProperty("startDate").GetString().ShouldBe("2026-08-21");
        body.GetProperty("endDate").GetString().ShouldBe("2026-11-01");

        var installments = body.GetProperty("installments").EnumerateArray().ToList();

        installments.Count.ShouldBe(3);

        installments.Select(installment => installment.GetProperty("amount").GetDecimal())
            .ShouldBe([33.33m, 33.33m, 33.34m]);

        installments.Sum(installment => installment.GetProperty("amount").GetDecimal())
            .ShouldBe(100.00m);

        installments.Select(installment => installment.GetProperty("installmentNumber").GetInt32())
            .ShouldBe([1, 2, 3]);

        installments.Select(installment => installment.GetProperty("competenceMonth").GetString())
            .ShouldBe(["2026-09-01", "2026-10-01", "2026-11-01"]);

        installments.ShouldAllBe(installment =>
            installment.GetProperty("type").GetInt32() == (int)ExpenseType.Credit);

        installments.ShouldAllBe(installment =>
            installment.GetProperty("date").GetString() == "2026-08-21");

        installments.ShouldAllBe(installment =>
            installment.GetProperty("installmentPlanId").GetGuid() == body.GetProperty("id").GetGuid());
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Installment_Count_Below_Two(string culture)
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.InstallmentCount = 1;

        var response = await DoPost(INSTALLMENT_PLAN, request, token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.INSTALLMENT_COUNT_INVALID), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Theory]
    [ClassData(typeof(CultureInlineDataTest))]
    public async Task Error_Total_Amount_Not_Greater_Than_Zero(string culture)
    {
        var caller = await NewAccount(closingDay: 20);

        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            caller.PersonId, caller.CategoryId, caller.AccountId);
        request.TotalAmount = 0;

        var response = await DoPost(INSTALLMENT_PLAN, request, token: caller.Token, culture: culture);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = (await ReadJson(response)).GetProperty("errorMessages").EnumerateArray();

        var expected = ResourceErrorMessages.ResourceManager.GetString(
            nameof(ResourceErrorMessages.AMOUNT_GREATER_THAN_ZERO), new CultureInfo(culture));

        errors.ShouldHaveSingleItem().GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task A_Category_Of_Another_User_Is_Not_Found()
    {
        var first = await NewAccount(closingDay: 20);
        var second = await NewAccount(closingDay: 20);

        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            second.PersonId, first.CategoryId, second.AccountId);

        var response = await DoPost(INSTALLMENT_PLAN, request, token: second.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_Installment_Plan_Without_Token_Is_Unauthorized()
    {
        var request = RequestRegisterInstallmentPlanJsonBuilder.Build(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var response = await DoPost(INSTALLMENT_PLAN, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record Caller(string Token, Guid PersonId, Guid CategoryId, Guid AccountId);

    private async Task<Caller> NewAccount(int? closingDay)
    {
        var registerResponse = await DoPost(USER, RequestRegisterUserJsonBuilder.Build());
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var token = (await ReadJson(registerResponse)).GetProperty("token").GetString()!;

        var people = (await ReadJson(await DoGet(PERSON, token: token)))
            .GetProperty("people").EnumerateArray().ToList();

        var personId = people[0].GetProperty("id").GetGuid();

        var categoryResponse = await DoPost(CATEGORY, RequestRegisterCategoryJsonBuilder.Build(), token: token);
        categoryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var accountRequest = RequestRegisterAccountJsonBuilder.Build(personId);
        accountRequest.ClosingDay = closingDay;

        var accountResponse = await DoPost(ACCOUNT, accountRequest, token: token);
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        return new Caller(
            token,
            personId,
            (await ReadJson(categoryResponse)).GetProperty("id").GetGuid(),
            (await ReadJson(accountResponse)).GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(body);

        return document.RootElement.Clone();
    }
}
