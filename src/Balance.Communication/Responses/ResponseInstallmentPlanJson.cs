namespace Balance.Communication.Responses;

public class ResponseInstallmentPlanJson
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }
    public DateOnly StartDate { get; set; }

    /// <summary>The competence month of the last installment, computed rather than supplied.</summary>
    public DateOnly EndDate { get; set; }

    public List<ResponseExpenseJson> Installments { get; set; } = [];
}
