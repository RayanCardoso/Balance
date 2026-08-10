using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.People;

public interface IPersonWriteOnlyRepository
{
    Task Add(Person person);
}
