namespace Balance.Communication.Enums;

/// <summary>
/// Wire-contract mirror of the domain enum. Communication references no other
/// project, so the two are kept in sync by their integer values.
/// </summary>
public enum IncomeType
{
    Recurring = 0,
    Variable = 1
}
