using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Accounts;

public interface IAccountWriteOnlyRepository
{
    Task Add(Account account);
}
