using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Person = Balance.Domain.Entities.Person;

namespace CommonTestUtilities.Entities;

public class IncomeSourceBuilder
{
    public static IncomeSource Recurring(
        Person person,
        decimal amount = 5000m,
        int expectedDay = 5,
        DateOnly? validityStart = null,
        string name = "Salary")
    {
        var source = new IncomeSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = IncomeType.Recurring,
            Archived = false,
            PersonId = person.Id,
            Person = person
        };

        source.Versions.Add(new IncomeSourceVersion
        {
            Id = Guid.NewGuid(),
            IncomeSourceId = source.Id,
            Amount = amount,
            ExpectedDay = expectedDay,
            ValidityStart = validityStart ?? new DateOnly(2026, 1, 1),
            ValidityEnd = null,
            ChangeReason = "initial"
        });

        return source;
    }

    public static IncomeSource Variable(Person person, string name = "Freelance")
    {
        return new IncomeSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = IncomeType.Variable,
            Archived = false,
            PersonId = person.Id,
            Person = person
        };
    }
}
