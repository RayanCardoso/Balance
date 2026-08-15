using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseRecurringExpenseJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public ExpenseType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }
    public int DueDay { get; set; }
    public bool IsEstimate { get; set; }
    public bool Archived { get; set; }

    /// <summary>
    /// The value history, oldest first. A freshly registered expense carries exactly one, open,
    /// version; a value change closes the previous one and appends the new one.
    /// </summary>
    public List<ResponseRecurringExpenseVersionJson> Versions { get; set; } = [];
}
