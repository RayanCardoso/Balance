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
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;

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
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ExpensePriority CategoryPriority { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;

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

    public string? Notes { get; set; }

    public ExpenseStatus Status { get; set; }
}
