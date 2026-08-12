using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseCategoryJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExpensePriority Priority { get; set; }
}
