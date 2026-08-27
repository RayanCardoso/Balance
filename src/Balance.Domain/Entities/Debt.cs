using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

public class Debt : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DebtMode Mode { get; set; }

    /// <summary>What was handed over. History only - it never enters an income total.</summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>What must be repaid. Equal to the principal on a family loan, higher on a bank loan.</summary>
    public decimal TotalAmount { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null on an OpenEnded debt, which has no schedule to be due against.</summary>
    public int? DueDay { get; set; }
    public int? InstallmentCount { get; set; }

    /// <summary>The competence month of the last installment. Computed, never accepted from a request.</summary>
    public DateOnly? EndMonth { get; set; }

    public bool Archived { get; set; }
    public string? Notes { get; set; }

    /// <summary>Who is owed.</summary>
    public Guid CreditorId { get; set; }
    public Creditor Creditor { get; set; } = null!;

    /// <summary>Who in the household owes it.</summary>
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public IList<DebtInstallment> Installments { get; set; } = [];
    public IList<DebtPayment> Payments { get; set; } = [];
}
