using Balance.Domain.Entities;
using Balance.Domain.Repositories.Expenses;

namespace Balance.Infrastructure.DataAccess.Repositories.Expenses;

internal class InstallmentPlanRepository : IInstallmentPlanWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public InstallmentPlanRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(InstallmentPlan plan) => await _dbContext.InstallmentPlans.AddAsync(plan);
}
