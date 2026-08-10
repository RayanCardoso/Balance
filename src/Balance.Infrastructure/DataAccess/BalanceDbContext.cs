using Balance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess;

public class BalanceDbContext : DbContext
{
    public BalanceDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
    }
}
