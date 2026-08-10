using Balance.Domain.Repositories;

namespace Balance.Infrastructure.DataAccess;

internal class UnitOfWork : IUnitOfWork
{
    private readonly BalanceDbContext _dbContext;

    public UnitOfWork(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Commit() => await _dbContext.SaveChangesAsync();
}
