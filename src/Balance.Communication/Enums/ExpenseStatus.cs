namespace Balance.Communication.Enums;

/// <summary>Wire-contract mirror of the domain enum, kept in sync by integer value.</summary>
public enum ExpenseStatus
{
    Pending = 0,
    Paid = 1,
    Divergent = 2
}
