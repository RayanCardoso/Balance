using Balance.Domain.Entities;
using Balance.Domain.Repositories.Incomes;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.Incomes;

internal class IncomeSourceRepository :
    IIncomeSourceReadOnlyRepository,
    IIncomeSourceWriteOnlyRepository,
    IIncomeSourceUpdateOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public IncomeSourceRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(IncomeSource incomeSource) => await _dbContext.IncomeSources.AddAsync(incomeSource);

    public async Task AddVersion(IncomeSourceVersion version) =>
        await _dbContext.IncomeSourceVersions.AddAsync(version);

    async Task<IncomeSource?> IIncomeSourceReadOnlyRepository.GetById(User user, Guid incomeSourceId) =>
        await _dbContext
            .IncomeSources
            .AsNoTracking()
            .Include(source => source.Versions)
            .FirstOrDefaultAsync(source =>
                source.Id == incomeSourceId && source.Person.UserId == user.Id);

    async Task<IncomeSource?> IIncomeSourceUpdateOnlyRepository.GetById(User user, Guid incomeSourceId) =>
        await _dbContext
            .IncomeSources
            .Include(source => source.Versions)
            .FirstOrDefaultAsync(source =>
                source.Id == incomeSourceId && source.Person.UserId == user.Id);

    public async Task<List<IncomeSource>> GetForMonth(User user, DateOnly referenceMonth)
    {
        var firstDayOfMonth = new DateOnly(referenceMonth.Year, referenceMonth.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        return await _dbContext
            .IncomeSources
            .AsNoTracking()
            .Include(source => source.Versions)
            .Include(source => source.Payments.Where(payment => payment.ReferenceMonth == firstDayOfMonth))
            .Where(source =>
                source.Person.UserId == user.Id &&
                !source.Archived &&
                source.Versions
                    .OrderByDescending(version => version.ValidityStart)
                    .Any(version =>
                        version.ValidityStart <= lastDayOfMonth &&
                        (version.ValidityEnd == null || version.ValidityEnd >= firstDayOfMonth)
                    )
            )
            .OrderBy(source => source.Name)
            .ToListAsync();
    }
}
