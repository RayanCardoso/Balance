using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseMonthlyIncomeJson
{
    public DateOnly ReferenceMonth { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal TotalReceived { get; set; }
    public List<ResponseMonthlyIncomeLineJson> Lines { get; set; } = [];
}

public class ResponseMonthlyIncomeLineJson
{
    public Guid IncomeSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IncomeType Type { get; set; }
    public Guid PersonId { get; set; }

    /// <summary>Null for a Variable source, which has no expected amount.</summary>
    public decimal? ExpectedAmount { get; set; }

    /// <summary>Null for a Variable source, which has no expected day.</summary>
    public int? ExpectedDay { get; set; }

    public decimal ReceivedAmount { get; set; }

    public IncomeStatus Status { get; set; }
}
