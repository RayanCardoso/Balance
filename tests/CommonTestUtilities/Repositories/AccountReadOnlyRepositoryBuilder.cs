using Balance.Domain.Entities;
using Balance.Domain.Repositories.Accounts;
using Moq;

namespace CommonTestUtilities.Repositories;

public class AccountReadOnlyRepositoryBuilder
{
    private readonly Mock<IAccountReadOnlyRepository> _repository = new();

    public AccountReadOnlyRepositoryBuilder GetAll(User user, List<Account> accounts)
    {
        _repository.Setup(repository => repository.GetAll(user)).ReturnsAsync(accounts);

        return this;
    }

    public AccountReadOnlyRepositoryBuilder GetById(User user, Account account)
    {
        _repository.Setup(repository => repository.GetById(user, account.Id)).ReturnsAsync(account);

        return this;
    }

    public IAccountReadOnlyRepository Build() => _repository.Object;
}
