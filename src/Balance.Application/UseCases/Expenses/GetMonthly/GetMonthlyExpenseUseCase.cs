using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories.Expenses;
using Balance.Domain.Repositories.RecurringExpenses;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using CommunicationExpensePriority = Balance.Communication.Enums.ExpensePriority;
using CommunicationExpenseStatus = Balance.Communication.Enums.ExpenseStatus;
using CommunicationExpenseType = Balance.Communication.Enums.ExpenseType;

namespace Balance.Application.UseCases.Expenses.GetMonthly;

public class GetMonthlyExpenseUseCase : IGetMonthlyExpenseUseCase
{
    private readonly IExpenseReadOnlyRepository _expenseReadOnlyRepository;
    private readonly IRecurringExpenseReadOnlyRepository _recurringExpenseReadOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetMonthlyExpenseUseCase(
        IExpenseReadOnlyRepository expenseReadOnlyRepository,
        IRecurringExpenseReadOnlyRepository recurringExpenseReadOnlyRepository,
        ILoggedUser loggedUser)
    {
        _expenseReadOnlyRepository = expenseReadOnlyRepository;
        _recurringExpenseReadOnlyRepository = recurringExpenseReadOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseMonthlyExpenseJson> Execute(int year, int month)
    {
        var competenceMonth = BuildCompetenceMonth(year, month);

        var loggedUser = await _loggedUser.Get();

        var expenses = await _expenseReadOnlyRepository.GetForMonth(loggedUser, competenceMonth);
        var recurringExpenses = await _recurringExpenseReadOnlyRepository.GetForMonth(loggedUser, competenceMonth);

        var variableLines = expenses.Select(BuildVariableLine).ToList();
        var recurringLines = recurringExpenses.Select(expense => BuildRecurringLine(expense, competenceMonth)).ToList();

        var totalVariable = variableLines.Sum(line => line.Amount);

        return new ResponseMonthlyExpenseJson
        {
            CompetenceMonth = competenceMonth,
            VariableLines = variableLines,
            RecurringLines = recurringLines,
            TotalVariable = totalVariable,
            TotalRecurringExpected = recurringLines.Sum(line => line.ExpectedAmount ?? 0m),
            TotalRecurringPaid = recurringLines.Sum(line => line.ActualAmount ?? 0m),
            TotalCommitted = totalVariable + recurringLines.Sum(Committed)
        };
    }

    private static ResponseVariableExpenseLineJson BuildVariableLine(Expense expense) =>
        new()
        {
            ExpenseId = expense.Id,
            Name = expense.Name,
            Type = (CommunicationExpenseType)expense.Type,
            Amount = expense.Amount,
            Date = expense.Date,
            PersonId = expense.PersonId,
            CategoryId = expense.CategoryId,
            CategoryName = expense.Category?.Name ?? string.Empty,
            CategoryPriority = (CommunicationExpensePriority)(expense.Category?.Priority ?? default),
            AccountId = expense.AccountId,
            AccountName = expense.Account?.Name ?? string.Empty,
            InstallmentNumber = expense.InstallmentNumber,
            InstallmentCount = expense.InstallmentPlan?.InstallmentCount,
            InstallmentPlanId = expense.InstallmentPlanId
        };

    private static ResponseRecurringExpenseLineJson BuildRecurringLine(
        RecurringExpense expense,
        DateOnly competenceMonth)
    {
        var version = expense.VersionInEffect(competenceMonth);
        var payment = expense.Payments.FirstOrDefault();

        return new ResponseRecurringExpenseLineJson
        {
            RecurringExpenseId = expense.Id,
            Name = expense.Name,
            PersonId = expense.PersonId,
            CategoryId = expense.CategoryId,
            AccountId = expense.AccountId,
            Type = (CommunicationExpenseType)(payment?.Type ?? expense.Type),
            DueDay = expense.DueDay,
            IsEstimate = expense.IsEstimate,
            ExpectedAmount = version?.Amount,
            ActualAmount = payment?.AmountPaid,
            PaymentDate = payment?.PaymentDate,
            PaymentId = payment?.Id,
            Notes = payment?.Notes,
            Status = ResolveStatus(version?.Amount, payment?.AmountPaid)
        };
    }

    /// <summary>
    /// What the month actually costs for one recurring line: the amount paid once the bill has
    /// arrived, and the estimate while it has not. A line with neither costs nothing yet.
    /// </summary>
    private static decimal Committed(ResponseRecurringExpenseLineJson line) =>
        line.ActualAmount ?? line.ExpectedAmount ?? 0m;

    /// <summary>
    /// Evaluated in this order so the rules cannot overlap: nothing paid is Pending; a bill with no
    /// version in effect is Paid once money went out, since it has no expectation to diverge from.
    /// </summary>
    private static CommunicationExpenseStatus ResolveStatus(decimal? expectedAmount, decimal? actualAmount)
    {
        if (actualAmount is null)
        {
            return CommunicationExpenseStatus.Pending;
        }

        if (expectedAmount is null)
        {
            return CommunicationExpenseStatus.Paid;
        }

        return actualAmount == expectedAmount
            ? CommunicationExpenseStatus.Paid
            : CommunicationExpenseStatus.Divergent;
    }

    private static DateOnly BuildCompetenceMonth(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            throw new ErrorOnValidationException([ResourceErrorMessages.REFERENCE_MONTH_INVALID]);
        }

        return new DateOnly(year, month, 1);
    }
}
