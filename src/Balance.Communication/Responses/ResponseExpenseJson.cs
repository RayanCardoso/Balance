using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseExpenseJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>The month this expense belongs to, normalised to the first day.</summary>
    public DateOnly CompetenceMonth { get; set; }

    /// <summary>Null unless this expense is one installment of a plan.</summary>
    public int? InstallmentNumber { get; set; }

    /// <summary>Null unless this expense is one installment of a plan.</summary>
    public Guid? InstallmentPlanId { get; set; }
}
