namespace Balance.Communication.Requests;

public class RequestRegisterInstallmentPlanJson
{
    public string Name { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    public decimal TotalAmount { get; set; }

    public int InstallmentCount { get; set; }

    public Guid CategoryId { get; set; }

    public Guid AccountId { get; set; }

    /// <summary>
    /// The date of the purchase. Every generated installment carries it, and the competence
    /// month of the first installment is derived from it.
    /// </summary>
    public DateOnly StartDate { get; set; }
}
