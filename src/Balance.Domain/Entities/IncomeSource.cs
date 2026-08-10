using Balance.Domain.Enums;

namespace Balance.Domain.Entities;

public class IncomeSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public IncomeType Type { get; set; }

    public bool Archived { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public IList<IncomeSourceVersion> Versions { get; set; } = [];
    public IList<IncomePayment> Payments { get; set; } = [];
}
