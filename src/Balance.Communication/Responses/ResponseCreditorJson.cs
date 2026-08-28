using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseCreditorJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CreditorType Type { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
    public bool Archived { get; set; }
}
