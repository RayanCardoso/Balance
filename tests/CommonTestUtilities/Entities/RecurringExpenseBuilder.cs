using Balance.Domain.Entities;
using Person = Balance.Domain.Entities.Person;

namespace CommonTestUtilities.Entities;

public class RecurringExpenseBuilder
{
    /// <summary>A recurring expense carrying one open version, as registration leaves it.</summary>
    public static RecurringExpense Build(
        Person person,
        decimal amount = 150m,
        int dueDay = 10,
        DateOnly? validityStart = null,
        bool isEstimate = true,
        bool archived = false,
        string name = "Luz")
    {
        var recurringExpense = new RecurringExpense
        {
            Id = Guid.NewGuid(),
            Name = name,
            DueDay = dueDay,
            IsEstimate = isEstimate,
            Archived = archived,
            PersonId = person.Id,
            Person = person,
            CategoryId = Guid.NewGuid(),
            AccountId = Guid.NewGuid()
        };

        recurringExpense.Versions.Add(new RecurringExpenseVersion
        {
            Id = Guid.NewGuid(),
            RecurringExpenseId = recurringExpense.Id,
            Amount = amount,
            ValidityStart = validityStart ?? new DateOnly(2026, 1, 1),
            ValidityEnd = null,
            ChangeReason = "initial"
        });

        return recurringExpense;
    }
}
