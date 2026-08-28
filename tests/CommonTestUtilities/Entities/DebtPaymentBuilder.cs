using Bogus;
using Balance.Domain.Entities;
using Balance.Domain.Enums;

namespace CommonTestUtilities.Entities;

public class DebtPaymentBuilder
{
    public static DebtPayment Build(
        Debt debt,
        Guid? debtInstallmentId = null,
        DateOnly? referenceMonth = null,
        decimal amountPaid = 100m,
        Guid? accountId = null,
        ExpenseType? type = null)
    {
        var month = referenceMonth ?? new DateOnly(2026, 8, 1);

        return new Faker<DebtPayment>()
            .RuleFor(p => p.Id, _ => Guid.NewGuid())
            .RuleFor(p => p.DebtId, _ => debt.Id)
            .RuleFor(p => p.Debt, _ => debt)
            .RuleFor(p => p.DebtInstallmentId, _ => debtInstallmentId)
            .RuleFor(p => p.ReferenceMonth, _ => month)
            .RuleFor(p => p.PaymentDate, _ => month.AddDays(9))
            .RuleFor(p => p.AmountPaid, _ => amountPaid)
            .RuleFor(p => p.Type, _ => type)
            .RuleFor(p => p.AccountId, _ => accountId)
            .RuleFor(p => p.Notes, faker => faker.Lorem.Sentence());
    }
}
