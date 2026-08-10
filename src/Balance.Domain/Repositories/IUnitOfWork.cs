namespace Balance.Domain.Repositories;

public interface IUnitOfWork
{
    Task Commit();
}
