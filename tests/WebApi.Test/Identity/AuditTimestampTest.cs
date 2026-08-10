using Balance.Infrastructure.DataAccess;
using CommonTestUtilities.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace WebApi.Test.Identity;

public class AuditTimestampTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditTimestampTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Added_Entity_Gets_CreatedAt_And_Null_UpdatedAt()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();

        var user = UserBuilder.Build();
        user.CreatedAt = default;
        user.UpdatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        user.CreatedAt.ShouldNotBe(default);
        user.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        user.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Modified_Entity_Gets_UpdatedAt()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();

        var user = UserBuilder.Build();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var createdAt = user.CreatedAt;

        user.Name = "changed name";
        await dbContext.SaveChangesAsync();

        user.UpdatedAt.ShouldNotBeNull();
        user.UpdatedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        user.UpdatedAt.Value.ShouldBeGreaterThanOrEqualTo(createdAt);
        user.CreatedAt.ShouldBe(createdAt);
    }
}
