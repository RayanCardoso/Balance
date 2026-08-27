using Bogus;
using Balance.Domain.Entities;

namespace CommonTestUtilities.Entities;

public class DebtInstallmentBuilder
{
    public static DebtInstallment Build(
        Debt debt,
        int number = 1,
        DateOnly? referenceMonth = null,
        DateOnly? dueDate = null,
        decimal expectedAmount = 100m)
    {
        var month = referenceMonth ?? new DateOnly(2026, 8, 1);

        return new Faker<DebtInstallment>()
            .RuleFor(i => i.Id, _ => Guid.NewGuid())
            .RuleFor(i => i.DebtId, _ => debt.Id)
            .RuleFor(i => i.Debt, _ => debt)
            .RuleFor(i => i.Number, _ => number)
            .RuleFor(i => i.ReferenceMonth, _ => month)
            .RuleFor(i => i.DueDate, _ => dueDate ?? month.AddDays(9))
            .RuleFor(i => i.ExpectedAmount, _ => expectedAmount);
    }
}
