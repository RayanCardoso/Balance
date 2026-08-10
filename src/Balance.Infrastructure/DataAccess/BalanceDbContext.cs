using Balance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Balance.Infrastructure.DataAccess;

public class BalanceDbContext : DbContext
{
    public BalanceDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<IncomeSource> IncomeSources { get; set; }
    public DbSet<IncomeSourceVersion> IncomeSourceVersions { get; set; }
    public DbSet<IncomePayment> IncomePayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();

        modelBuilder.Entity<Person>(person =>
        {
            person.HasIndex(p => p.UserId);

            person.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomeSource>(source =>
        {
            source.HasIndex(s => s.PersonId);

            source.HasOne(s => s.Person)
                .WithMany()
                .HasForeignKey(s => s.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomeSourceVersion>(version =>
        {
            version.Property(v => v.Amount).HasPrecision(18, 2);

            version.HasIndex(v => new { v.IncomeSourceId, v.ValidityStart });

            version.HasOne(v => v.IncomeSource)
                .WithMany(s => s.Versions)
                .HasForeignKey(v => v.IncomeSourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomePayment>(payment =>
        {
            payment.Property(p => p.AmountReceived).HasPrecision(18, 2);

            payment.HasIndex(p => new { p.IncomeSourceId, p.ReferenceMonth });

            payment.HasOne(p => p.IncomeSource)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.IncomeSourceId)
                .OnDelete(DeleteBehavior.Restrict);

            payment.HasOne(p => p.IncomeSourceVersion)
                .WithMany()
                .HasForeignKey(p => p.IncomeSourceVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        StampAuditFields();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
