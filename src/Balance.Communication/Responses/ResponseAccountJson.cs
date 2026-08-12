namespace Balance.Communication.Responses;

public class ResponseAccountJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public int? ClosingDay { get; set; }
    public int? DueDay { get; set; }
    public decimal? Limit { get; set; }
}
