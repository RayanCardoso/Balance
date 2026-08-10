using Balance.Domain.Entities;
using Balance.Domain.Repositories.People;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.People;

internal class PersonRepository : IPersonReadOnlyRepository, IPersonWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public PersonRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Person person) => await _dbContext.People.AddAsync(person);

    public async Task<List<Person>> GetAll(User user) =>
        await _dbContext
            .People
            .AsNoTracking()
            .Where(person => person.UserId == user.Id)
            .OrderBy(person => person.Name)
            .ToListAsync();

    public async Task<Person?> GetById(User user, Guid personId) =>
        await _dbContext
            .People
            .AsNoTracking()
            .FirstOrDefaultAsync(person => person.Id == personId && person.UserId == user.Id);
}
