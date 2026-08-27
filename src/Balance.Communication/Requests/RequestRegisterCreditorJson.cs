using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterCreditorJson
{
    public string Name { get; set; } = string.Empty;

    public CreditorType Type { get; set; }

    public string? Contact { get; set; }

    public string? Notes { get; set; }
}
