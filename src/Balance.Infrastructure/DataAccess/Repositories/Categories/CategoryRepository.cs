using Balance.Domain.Entities;
using Balance.Domain.Repositories.Categories;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess.Repositories.Categories;

internal class CategoryRepository : ICategoryReadOnlyRepository, ICategoryWriteOnlyRepository
{
    private readonly BalanceDbContext _dbContext;

    public CategoryRepository(BalanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Category category) => await _dbContext.Categories.AddAsync(category);

    public async Task<List<Category>> GetAll(User user) =>
        await _dbContext
            .Categories
            .AsNoTracking()
            .Where(category => category.UserId == user.Id)
            .OrderBy(category => category.Name)
            .ToListAsync();

    public async Task<Category?> GetById(User user, Guid categoryId) =>
        await _dbContext
            .Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == categoryId && category.UserId == user.Id);
}
