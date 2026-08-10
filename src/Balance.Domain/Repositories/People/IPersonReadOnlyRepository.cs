using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.People;

public interface IPersonReadOnlyRepository
{
    Task<List<Person>> GetAll(User user);

    Task<Person?> GetById(User user, Guid personId);
}
