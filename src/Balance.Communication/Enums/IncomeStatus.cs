namespace Balance.Communication.Enums;

/// <summary>Wire-contract mirror of the domain enum, kept in sync by integer value.</summary>
public enum IncomeStatus
{
    Pending = 0,
    Received = 1,
    Divergent = 2
}
