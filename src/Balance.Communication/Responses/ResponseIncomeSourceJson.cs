using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseIncomeSourceJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IncomeType Type { get; set; }
    public Guid PersonId { get; set; }
    public bool Archived { get; set; }

    /// <summary>Null for a Variable source, which has no version.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Null for a Variable source, which has no version.</summary>
    public int? ExpectedDay { get; set; }
}
