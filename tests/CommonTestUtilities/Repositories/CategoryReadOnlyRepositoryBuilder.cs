using Balance.Domain.Entities;
using Balance.Domain.Repositories.Categories;
using Moq;

namespace CommonTestUtilities.Repositories;

public class CategoryReadOnlyRepositoryBuilder
{
    private readonly Mock<ICategoryReadOnlyRepository> _repository = new();

    public CategoryReadOnlyRepositoryBuilder GetAll(User user, List<Category> categories)
    {
        _repository.Setup(repository => repository.GetAll(user)).ReturnsAsync(categories);

        return this;
    }

    public CategoryReadOnlyRepositoryBuilder GetById(User user, Category category)
    {
        _repository.Setup(repository => repository.GetById(user, category.Id)).ReturnsAsync(category);

        return this;
    }

    public ICategoryReadOnlyRepository Build() => _repository.Object;
}
