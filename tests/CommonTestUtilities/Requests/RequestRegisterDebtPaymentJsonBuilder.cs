using Bogus;
using Balance.Communication.Enums;
using Balance.Communication.Requests;

namespace CommonTestUtilities.Requests;

public class RequestRegisterDebtPaymentJsonBuilder
{
    public static RequestRegisterDebtPaymentJson Build(
        Guid debtId,
        Guid? debtInstallmentId = null,
        DateOnly? paymentDate = null,
        Guid? accountId = null,
        ExpenseType? type = null)
    {
        var date = paymentDate ?? new DateOnly(2026, 8, 10);

        return new Faker<RequestRegisterDebtPaymentJson>()
            .RuleFor(r => r.DebtId, _ => debtId)
            .RuleFor(r => r.DebtInstallmentId, _ => debtInstallmentId)
            .RuleFor(r => r.PaymentDate, _ => date)
            .RuleFor(r => r.AmountPaid, faker => faker.Random.Decimal(50, 500))
            .RuleFor(r => r.Notes, faker => faker.Lorem.Sentence())
            .RuleFor(r => r.AccountId, _ => accountId)
            .RuleFor(r => r.Type, _ => type);
    }
}
