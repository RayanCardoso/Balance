using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Security.Cryptography;
using Balance.Domain.Security.Tokens;
using Balance.Infrastructure.DataAccess;
using CommonTestUtilities.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Test.Resources;

namespace WebApi.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public UserIdentityManager User_Team_Member { get; private set; } = default!;
    public UserIdentityManager User_Admin { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test")
            .ConfigureServices(services =>
            {
                var provider = services.AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

                services.AddDbContext<BalanceDbContext>(config =>
                {
                    config.UseInMemoryDatabase("InMemoryDbForTesting");
                    config.UseInternalServiceProvider(provider);
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();
                var passwordEncripter = scope.ServiceProvider.GetRequiredService<IPasswordEncripter>();
                var accessTokenGenerator = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();

                StartDatabase(dbContext, passwordEncripter, accessTokenGenerator);
            });
    }

    private void StartDatabase(
        BalanceDbContext dbContext,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator)
    {
        User_Team_Member = AddUser(dbContext, passwordEncripter, accessTokenGenerator, Roles.TEAM_MEMBER);
        User_Admin = AddUser(dbContext, passwordEncripter, accessTokenGenerator, Roles.ADMIN);

        dbContext.SaveChanges();
    }

    private static UserIdentityManager AddUser(
        BalanceDbContext dbContext,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator,
        string role)
    {
        var user = UserBuilder.Build(role);

        var plainPassword = user.Password;
        user.Password = passwordEncripter.Encrypt(plainPassword);

        dbContext.Users.Add(user);

        return new UserIdentityManager(user, plainPassword, accessTokenGenerator.Generate(user));
    }
}
