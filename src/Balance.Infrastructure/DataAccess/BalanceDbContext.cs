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
    public DbSet<Category> Categories { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<InstallmentPlan> InstallmentPlans { get; set; }
    public DbSet<RecurringExpense> RecurringExpenses { get; set; }
    public DbSet<RecurringExpenseVersion> RecurringExpenseVersions { get; set; }
    public DbSet<RecurringExpensePayment> RecurringExpensePayments { get; set; }

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

        modelBuilder.Entity<Category>(category =>
        {
            category.HasIndex(c => c.UserId);

            category.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(account =>
        {
            account.Property(a => a.Limit).HasPrecision(18, 2);

            account.HasIndex(a => a.PersonId);

            account.HasOne(a => a.Person)
                .WithMany()
                .HasForeignKey(a => a.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(expense =>
        {
            expense.Property(e => e.Amount).HasPrecision(18, 2);

            expense.HasIndex(e => new { e.PersonId, e.CompetenceMonth });
            expense.HasIndex(e => e.InstallmentPlanId);

            expense.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            expense.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            expense.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            expense.HasOne(e => e.InstallmentPlan)
                .WithMany(p => p.Installments)
                .HasForeignKey(e => e.InstallmentPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstallmentPlan>(plan =>
        {
            plan.Property(p => p.TotalAmount).HasPrecision(18, 2);

            plan.HasIndex(p => p.PersonId);

            plan.HasOne(p => p.Person)
                .WithMany()
                .HasForeignKey(p => p.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            plan.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            plan.HasOne(p => p.Account)
                .WithMany()
                .HasForeignKey(p => p.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringExpense>(recurring =>
        {
            recurring.HasIndex(r => r.PersonId);

            recurring.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            recurring.HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            recurring.HasOne(r => r.Account)
                .WithMany()
                .HasForeignKey(r => r.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringExpenseVersion>(version =>
        {
            version.Property(v => v.Amount).HasPrecision(18, 2);

            version.HasIndex(v => new { v.RecurringExpenseId, v.ValidityStart });

            version.HasOne(v => v.RecurringExpense)
                .WithMany(r => r.Versions)
                .HasForeignKey(v => v.RecurringExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringExpensePayment>(payment =>
        {
            payment.Property(p => p.AmountPaid).HasPrecision(18, 2);

            payment.HasIndex(p => new { p.RecurringExpenseId, p.ReferenceMonth }).IsUnique();

            payment.HasOne(p => p.RecurringExpense)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RecurringExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            payment.HasOne(p => p.Version)
                .WithMany()
                .HasForeignKey(p => p.RecurringExpenseVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            payment.HasOne(p => p.Account)
                .WithMany()
                .HasForeignKey(p => p.AccountId)
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
