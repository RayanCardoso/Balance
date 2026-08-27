using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseDebtJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DebtMode Mode { get; set; }

    /// <summary>Who is owed.</summary>
    public Guid CreditorId { get; set; }
    public string CreditorName { get; set; } = string.Empty;
    public CreditorType CreditorType { get; set; }

    /// <summary>Who in the household owes it.</summary>
    public Guid PersonId { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>What was handed over. History only - it never enters an income total.</summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>What must be repaid. Equal to the principal on a family loan, higher on a bank loan.</summary>
    public decimal TotalAmount { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null on an OpenEnded debt, which has no schedule to be due against.</summary>
    public int? DueDay { get; set; }

    /// <summary>Null on an OpenEnded debt, which has no schedule to be due against.</summary>
    public int? InstallmentCount { get; set; }

    /// <summary>The competence month of the last installment. Null on an OpenEnded debt.</summary>
    public DateOnly? EndMonth { get; set; }

    public bool Archived { get; set; }
    public string? Notes { get; set; }

    /// <summary>Computed by DebtExtensions - never stored, never accepted from a request.</summary>
    public decimal OutstandingBalance { get; set; }

    /// <summary>Computed by DebtExtensions - never stored, never accepted from a request.</summary>
    public bool IsSettled { get; set; }

    public List<ResponseDebtInstallmentJson> Installments { get; set; } = [];
}
