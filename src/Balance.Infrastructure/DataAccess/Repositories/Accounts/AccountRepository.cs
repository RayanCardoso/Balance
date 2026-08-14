using Balance.Domain.Entities;
using Balance.Domain.Repositories.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.Accounts;

internal class AccountRepository : IAccountReadOnlyRepository, IAccountWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public AccountRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Account account) => await _dbContext.Accounts.AddAsync(account);

    public async Task<List<Account>> GetAll(User user) =>
        await _dbContext
            .Accounts
            .AsNoTracking()
            .Where(account => account.Person.UserId == user.Id)
            .OrderBy(account => account.Name)
            .ToListAsync();

    public async Task<Account?> GetById(User user, Guid accountId) =>
        await _dbContext
            .Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == accountId && account.Person.UserId == user.Id);
}
