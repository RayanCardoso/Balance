using Balance.Domain.Entities;

namespace Balance.Domain.Services.LoggedUser;

public interface ILoggedUser
{
    Task<User> Get();
}
