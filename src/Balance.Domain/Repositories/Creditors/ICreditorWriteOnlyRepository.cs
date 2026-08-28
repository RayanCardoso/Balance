using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Creditors;

public interface ICreditorWriteOnlyRepository
{
    Task Add(Creditor creditor);
}
