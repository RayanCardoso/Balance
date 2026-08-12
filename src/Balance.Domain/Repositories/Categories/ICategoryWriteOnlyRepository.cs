using Balance.Domain.Entities;

namespace Balance.Domain.Repositories.Categories;

public interface ICategoryWriteOnlyRepository
{
    Task Add(Category category);
}
