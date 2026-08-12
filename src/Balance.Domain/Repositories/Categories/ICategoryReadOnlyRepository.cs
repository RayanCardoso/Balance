using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Categories;

public interface ICategoryReadOnlyRepository
{
    Task<List<Category>> GetAll(User user);

    Task<Category?> GetById(User user, Guid categoryId);
}
