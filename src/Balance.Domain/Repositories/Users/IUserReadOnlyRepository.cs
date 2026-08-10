namespace Balance.Domain.Repositories.Users;

public interface IUserReadOnlyRepository
{
    Task<bool> ExistActiveUserWithEmail(string email);
    Task<Entities.User?> GetByEmail(string email);
}
