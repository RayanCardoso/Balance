using Balance.Domain.Entities;
using Balance.Domain.Repositories.Creditors;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.Creditors;

internal class CreditorRepository :
    ICreditorReadOnlyRepository,
    ICreditorWriteOnlyRepository,
    ICreditorUpdateOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public CreditorRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Creditor creditor) => await _dbContext.Creditors.AddAsync(creditor);

    public async Task<List<Creditor>> GetAll(User user, bool includeArchived) =>
        await _dbContext
            .Creditors
            .AsNoTracking()
            .Where(creditor => creditor.UserId == user.Id && (includeArchived || creditor.Archived == false))
            .OrderBy(creditor => creditor.Name)
            .ToListAsync();

    async Task<Creditor?> ICreditorReadOnlyRepository.GetById(User user, Guid id) =>
        await _dbContext
            .Creditors
            .AsNoTracking()
            .FirstOrDefaultAsync(creditor => creditor.Id == id && creditor.UserId == user.Id);

    async Task<Creditor?> ICreditorUpdateOnlyRepository.GetById(User user, Guid id) =>
        await _dbContext
            .Creditors
            .FirstOrDefaultAsync(creditor => creditor.Id == id && creditor.UserId == user.Id);
}
