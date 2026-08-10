namespace Balance.Communication.Responses;

public class ResponsePersonJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAccountOwner { get; set; }
}
