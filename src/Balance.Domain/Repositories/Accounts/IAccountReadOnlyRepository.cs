using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Accounts;

public interface IAccountReadOnlyRepository
{
    Task<List<Account>> GetAll(User user);

    Task<Account?> GetById(User user, Guid accountId);
}
