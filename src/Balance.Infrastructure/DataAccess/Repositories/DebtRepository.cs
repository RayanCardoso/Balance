using Balance.Domain.Entities;
using Balance.Domain.Repositories.Debts;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories;

internal class DebtRepository :
    IDebtReadOnlyRepository,
    IDebtWriteOnlyRepository,
    IDebtUpdateOnlyRepository,
    IDebtInstallmentWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public DebtRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Debt debt) => await _dbContext.Debts.AddAsync(debt);

    public async Task AddRange(IEnumerable<DebtInstallment> installments) =>
        await _dbContext.DebtInstallments.AddRangeAsync(installments);

    public async Task<List<Debt>> GetAll(User user, Guid? creditorId, Guid? personId, bool includeInactive)
    {
        var query = _dbContext
            .Debts
            .AsNoTracking()
            .Include(debt => debt.Creditor)
            .Include(debt => debt.Category)
            .Include(debt => debt.Payments.OrderBy(payment => payment.PaymentDate))
            .Where(debt => debt.Person.UserId == user.Id);

        if (creditorId.HasValue)
        {
            query = query.Where(debt => debt.CreditorId == creditorId.Value);
        }

        if (personId.HasValue)
        {
            query = query.Where(debt => debt.PersonId == personId.Value);
        }

        if (includeInactive == false)
        {
            query = query.Where(debt => debt.Archived == false);
        }

        return await query.OrderByDescending(debt => debt.StartDate).ToListAsync();
    }

    async Task<Debt?> IDebtReadOnlyRepository.GetById(User user, Guid id) =>
        await _dbContext
            .Debts
            .AsNoTracking()
            .Include(debt => debt.Creditor)
            .Include(debt => debt.Category)
            .Include(debt => debt.Installments.OrderBy(installment => installment.Number))
            .Include(debt => debt.Payments.OrderBy(payment => payment.PaymentDate))
            .FirstOrDefaultAsync(debt => debt.Id == id && debt.Person.UserId == user.Id);

    async Task<Debt?> IDebtUpdateOnlyRepository.GetById(User user, Guid id) =>
        await _dbContext
            .Debts
            .Include(debt => debt.Installments.OrderBy(installment => installment.Number))
            .Include(debt => debt.Payments.OrderBy(payment => payment.PaymentDate))
            .FirstOrDefaultAsync(debt => debt.Id == id && debt.Person.UserId == user.Id);

    public async Task<List<Debt>> GetForMonth(User user, DateOnly competenceMonth)
    {
        var firstDayOfMonth = new DateOnly(competenceMonth.Year, competenceMonth.Month, 1);

        return await _dbContext
            .Debts
            .AsNoTracking()
            .Include(debt => debt.Creditor)
            .Include(debt => debt.Category)
            .Include(debt => debt.Installments
                .Where(installment => installment.ReferenceMonth == firstDayOfMonth)
                .OrderBy(installment => installment.Number))
            .Include(debt => debt.Payments
                .Where(payment => payment.ReferenceMonth == firstDayOfMonth)
                .OrderBy(payment => payment.PaymentDate))
            .Where(debt =>
                debt.Person.UserId == user.Id
                && debt.Archived == false
                && (debt.Installments.Any(installment => installment.ReferenceMonth == firstDayOfMonth)
                    || debt.Payments.Any(payment => payment.ReferenceMonth == firstDayOfMonth)))
            .OrderBy(debt => debt.Name)
            .ToListAsync();
    }

    public async Task<List<Debt>> GetByCreditor(User user, Guid creditorId) =>
        await _dbContext
            .Debts
            .AsNoTracking()
            .Include(debt => debt.Payments.OrderBy(payment => payment.PaymentDate))
            .Where(debt => debt.CreditorId == creditorId && debt.Person.UserId == user.Id)
            .OrderByDescending(debt => debt.StartDate)
            .ToListAsync();
}
