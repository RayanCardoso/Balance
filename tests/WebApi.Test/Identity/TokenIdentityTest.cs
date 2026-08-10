using Balance.Infrastructure.DataAccess;
using CommonTestUtilities.Token;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Test.Identity;

public class TokenIdentityTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TokenIdentityTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Sid_Claim_Carries_The_User_Id()
    {
        var expectedId = _factory.User_Team_Member.GetId();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(_factory.User_Team_Member.GetToken());

        var sid = token.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value;

        sid.ShouldBe(expectedId.ToString());
    }

    [Fact]
    public async Task LoggedUser_Resolves_The_User_Matching_The_Sid_Claim()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();

        var tokenProvider = TokenProviderBuilder.Build(_factory.User_Admin.GetToken());

        var loggedUser = new Balance.Infrastructure.Services.LoggedUser.LoggedUser(dbContext, tokenProvider);

        var user = await loggedUser.Get();

        user.Id.ShouldBe(_factory.User_Admin.GetId());
        user.Email.ShouldBe(_factory.User_Admin.GetEmail());
    }
}
