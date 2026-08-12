using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterCategoryJson
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ExpensePriority Priority { get; set; }
}
