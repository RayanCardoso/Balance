using Balance.Communication.Enums;

namespace Balance.Communication.Responses;

public class ResponseMonthlyExpenseJson
{
    public DateOnly CompetenceMonth { get; set; }

    /// <summary>One-off and installment expenses whose competence month is this one.</summary>
    public List<ResponseVariableExpenseLineJson> VariableLines { get; set; } = [];

    /// <summary>Every non-archived recurring expense, paid or not.</summary>
    public List<ResponseRecurringExpenseLineJson> RecurringLines { get; set; } = [];

    public decimal TotalVariable { get; set; }

    public decimal TotalRecurringExpected { get; set; }

    public decimal TotalRecurringPaid { get; set; }

    /// <summary>
    /// What the month actually costs: the variable total plus, per recurring line, the amount paid
    /// when the bill has arrived and the estimate when it has not.
    /// </summary>
    public decimal TotalCommitted { get; set; }
}

public class ResponseVariableExpenseLineJson
{
    public Guid ExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }

    public Guid PersonId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ExpensePriority CategoryPriority { get; set; }
    /// <summary>Null when the expense was not paid from a registered account.</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Null when there is no account — never an empty string, which would read as a
    /// nameless account rather than as no account at all.</summary>
    public string? AccountName { get; set; }

    /// <summary>Null unless this expense is one installment of a plan.</summary>
    public int? InstallmentNumber { get; set; }

    /// <summary>Null unless this expense is one installment of a plan.</summary>
    public int? InstallmentCount { get; set; }

    public Guid? InstallmentPlanId { get; set; }
}

public class ResponseRecurringExpenseLineJson
{
    public Guid RecurringExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid PersonId { get; set; }

    /// <summary>
    /// Identifiers only. The category and account names carried by a variable line are not repeated
    /// here: <c>GetForMonth</c> loads a recurring expense with its versions and the month's payment,
    /// not its catalogue navigations, and the spec asks a recurring line for the expected amount, the
    /// due day, <c>IsEstimate</c>, the actual and the status - not for catalogue names.
    /// </summary>
    public Guid CategoryId { get; set; }
    public Guid? AccountId { get; set; }

    /// <summary>The month's payment override when present, otherwise the recurring expense's own type.</summary>
    public ExpenseType Type { get; set; }

    public int DueDay { get; set; }

    /// <summary>Marks the expected amount as provisional rather than a known fixed value.</summary>
    public bool IsEstimate { get; set; }

    /// <summary>
    /// The amount of the version in effect at this month. Null when the month predates every version.
    /// </summary>
    public decimal? ExpectedAmount { get; set; }

    /// <summary>What was actually paid this month. Null while the bill has not arrived.</summary>
    public decimal? ActualAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }

    /// <summary>
    /// The id of this month's payment, null while the bill has not arrived. Without it a client
    /// reading a monthly line cannot reach <c>PUT /api/recurring-expense/payment/{id}</c>, so a
    /// payment recorded in an earlier session could never be corrected.
    /// </summary>
    public Guid? PaymentId { get; set; }

    public string? Notes { get; set; }

    public ExpenseStatus Status { get; set; }
}
