using Balance.Communication.Enums;

namespace Balance.Communication.Requests;

public class RequestRegisterDebtJson
{
    public string Name { get; set; } = string.Empty;
    public Guid CreditorId { get; set; }
    public Guid PersonId { get; set; }
    public Guid CategoryId { get; set; }
    public DebtMode Mode { get; set; }

    /// <summary>What is handed over. History only - it never enters an income total.</summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>What must be repaid. Equal to the principal on a family loan, higher on a bank loan.</summary>
    public decimal TotalAmount { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null for an OpenEnded debt, which has no schedule to be due against.</summary>
    public int? InstallmentCount { get; set; }

    /// <summary>Null for an OpenEnded debt, which has no schedule to be due against.</summary>
    public int? DueDay { get; set; }

    public string? Notes { get; set; }
}
