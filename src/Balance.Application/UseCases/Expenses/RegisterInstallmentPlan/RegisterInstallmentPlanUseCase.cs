using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Repositories.Expenses;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Expenses.RegisterInstallmentPlan;

public class RegisterInstallmentPlanUseCase : IRegisterInstallmentPlanUseCase
{
    private readonly IInstallmentPlanWriteOnlyRepository _planWriteOnlyRepository;
    private readonly IExpenseWriteOnlyRepository _expenseWriteOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly ICategoryReadOnlyRepository _categoryReadOnlyRepository;
    private readonly IAccountReadOnlyRepository _accountReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterInstallmentPlanUseCase(
        IInstallmentPlanWriteOnlyRepository planWriteOnlyRepository,
        IExpenseWriteOnlyRepository expenseWriteOnlyRepository,
        IPersonReadOnlyRepository personReadOnlyRepository,
        ICategoryReadOnlyRepository categoryReadOnlyRepository,
        IAccountReadOnlyRepository accountReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _planWriteOnlyRepository = planWriteOnlyRepository;
        _expenseWriteOnlyRepository = expenseWriteOnlyRepository;
        _personReadOnlyRepository = personReadOnlyRepository;
        _categoryReadOnlyRepository = categoryReadOnlyRepository;
        _accountReadOnlyRepository = accountReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseInstallmentPlanJson> Execute(RequestRegisterInstallmentPlanJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var category = await _categoryReadOnlyRepository.GetById(loggedUser, request.CategoryId)
            ?? throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);

        var account = await _accountReadOnlyRepository.GetById(loggedUser, request.AccountId)
            ?? throw new NotFoundException(ResourceErrorMessages.ACCOUNT_NOT_FOUND);

        // Installment 1 uses the same rule as a single credit expense; each later one advances a month.
        var firstCompetenceMonth =
            CompetenceMonthResolver.Resolve(ExpenseType.Credit, account.ClosingDay, request.StartDate);

        var count = request.InstallmentCount;

        var plan = new InstallmentPlan
        {
            Name = request.Name,
            TotalAmount = request.TotalAmount,
            InstallmentCount = count,
            StartDate = request.StartDate,
            EndDate = firstCompetenceMonth.AddMonths(count - 1),
            PersonId = person.Id,
            CategoryId = category.Id,
            AccountId = account.Id
        };

        var installments = BuildInstallments(request, plan, firstCompetenceMonth);

        await _planWriteOnlyRepository.Add(plan);
        await _expenseWriteOnlyRepository.AddRange(installments);
        await _unitOfWork.Commit();

        return new ResponseInstallmentPlanJson
        {
            Id = plan.Id,
            Name = plan.Name,
            PersonId = plan.PersonId,
            TotalAmount = plan.TotalAmount,
            InstallmentCount = plan.InstallmentCount,
            CategoryId = plan.CategoryId,
            AccountId = plan.AccountId,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            Installments = installments.ConvertAll(installment => new ResponseExpenseJson
            {
                Id = installment.Id,
                Name = installment.Name,
                PersonId = installment.PersonId,
                Type = (Communication.Enums.ExpenseType)installment.Type,
                Amount = installment.Amount,
                CategoryId = installment.CategoryId,
                AccountId = installment.AccountId,
                Date = installment.Date,
                CompetenceMonth = installment.CompetenceMonth,
                InstallmentNumber = installment.InstallmentNumber,
                InstallmentPlanId = installment.InstallmentPlanId
            })
        };
    }

    /// <summary>
    /// Installments 1..N-1 carry the rounded share; installment N carries the residual, so the
    /// N amounts sum to the total exactly rather than by rounding luck.
    /// </summary>
    private static List<Expense> BuildInstallments(
        RequestRegisterInstallmentPlanJson request,
        InstallmentPlan plan,
        DateOnly firstCompetenceMonth)
    {
        var count = request.InstallmentCount;
        var each = Math.Round(request.TotalAmount / count, 2, MidpointRounding.AwayFromZero);
        var residual = request.TotalAmount - each * (count - 1);

        var installments = new List<Expense>(count);

        for (var number = 1; number <= count; number++)
        {
            installments.Add(new Expense
            {
                Name = request.Name,
                Type = ExpenseType.Credit,
                Amount = number == count ? residual : each,
                Date = request.StartDate,
                CompetenceMonth = firstCompetenceMonth.AddMonths(number - 1),
                InstallmentNumber = number,
                PersonId = plan.PersonId,
                CategoryId = plan.CategoryId,
                AccountId = plan.AccountId,
                InstallmentPlanId = plan.Id
            });
        }

        return installments;
    }

    private static void Validate(RequestRegisterInstallmentPlanJson request)
    {
        var result = new RegisterInstallmentPlanValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
