using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterExpenseJson
{
    public string Name { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    public ExpenseType Type { get; set; }

    public decimal Amount { get; set; }

    public Guid CategoryId { get; set; }

    public Guid AccountId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>
    /// Optional override of the competence month derived from the account's closing day.
    /// Any day inside the month it refers to; normalised to the first day.
    /// </summary>
    public DateOnly? CompetenceMonth { get; set; }
}
