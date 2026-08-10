using Balance.Domain.Entities;
using Balance.Domain.Security.Tokens;
using Balance.Domain.Services.LoggedUser;
using Balance.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Balance.Infrastructure.Services.LoggedUser;

public class LoggedUser : ILoggedUser
{
    private readonly BalanceDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;

    public LoggedUser(BalanceDbContext dbContext, ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }

    public async Task<User> Get()
    {
        var token = _tokenProvider.TokenOnRequest();

        var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var identifier = jwtSecurityToken.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value;

        return await _dbContext
            .Users
            .AsNoTracking()
            .FirstAsync(user => user.UserIdentifier == Guid.Parse(identifier));
    }
}
