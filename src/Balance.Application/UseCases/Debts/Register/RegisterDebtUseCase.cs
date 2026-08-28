using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Enums;
using Balance.Domain.Extensions;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Categories;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Repositories.Debts;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Debts.Register;

public class RegisterDebtUseCase : IRegisterDebtUseCase
{
    private readonly IDebtWriteOnlyRepository _debtWriteOnlyRepository;
    private readonly IDebtInstallmentWriteOnlyRepository _debtInstallmentWriteOnlyRepository;
    private readonly ICreditorReadOnlyRepository _creditorReadOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly ICategoryReadOnlyRepository _categoryReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterDebtUseCase(
        IDebtWriteOnlyRepository debtWriteOnlyRepository,
        IDebtInstallmentWriteOnlyRepository debtInstallmentWriteOnlyRepository,
        ICreditorReadOnlyRepository creditorReadOnlyRepository,
        IPersonReadOnlyRepository personReadOnlyRepository,
        ICategoryReadOnlyRepository categoryReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _debtWriteOnlyRepository = debtWriteOnlyRepository;
        _debtInstallmentWriteOnlyRepository = debtInstallmentWriteOnlyRepository;
        _creditorReadOnlyRepository = creditorReadOnlyRepository;
        _personReadOnlyRepository = personReadOnlyRepository;
        _categoryReadOnlyRepository = categoryReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseDebtJson> Execute(RequestRegisterDebtJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        // Three separate lookups, three separate not-found keys - a foreign category must never
        // be reported as a missing person, or vice versa.
        var creditor = await _creditorReadOnlyRepository.GetById(loggedUser, request.CreditorId)
            ?? throw new NotFoundException(ResourceErrorMessages.CREDITOR_NOT_FOUND);

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var category = await _categoryReadOnlyRepository.GetById(loggedUser, request.CategoryId)
            ?? throw new NotFoundException(ResourceErrorMessages.CATEGORY_NOT_FOUND);

        var debt = new Debt
        {
            Name = request.Name,
            Mode = (DebtMode)request.Mode,
            PrincipalAmount = request.PrincipalAmount,
            TotalAmount = request.TotalAmount,
            StartDate = request.StartDate,
            Notes = request.Notes,
            CreditorId = creditor.Id,
            PersonId = person.Id,
            CategoryId = category.Id
        };

        var installments = debt.Mode == DebtMode.Scheduled
            ? BuildInstallments(request, debt)
            : [];

        // Debt and installments are written together and committed once - a schedule that half
        // saved would be worse than a debt that failed to register at all.
        await _debtWriteOnlyRepository.Add(debt);
        await _debtInstallmentWriteOnlyRepository.AddRange(installments);
        await _unitOfWork.Commit();

        return new ResponseDebtJson
        {
            Id = debt.Id,
            Name = debt.Name,
            Mode = request.Mode,
            CreditorId = debt.CreditorId,
            CreditorName = creditor.Name,
            CreditorType = (Communication.Enums.CreditorType)creditor.Type,
            PersonId = debt.PersonId,
            CategoryId = debt.CategoryId,
            CategoryName = category.Name,
            PrincipalAmount = debt.PrincipalAmount,
            TotalAmount = debt.TotalAmount,
            StartDate = debt.StartDate,
            DueDay = debt.DueDay,
            InstallmentCount = debt.InstallmentCount,
            EndMonth = debt.EndMonth,
            Archived = debt.Archived,
            Notes = debt.Notes,
            OutstandingBalance = debt.OutstandingBalance(),
            IsSettled = debt.IsSettled(),
            Installments = installments.ConvertAll(installment => new ResponseDebtInstallmentJson
            {
                Id = installment.Id,
                Number = installment.Number,
                ReferenceMonth = installment.ReferenceMonth,
                DueDate = installment.DueDate,
                ExpectedAmount = installment.ExpectedAmount,
                AmountPaid = null,
                PaymentId = null,
                Status = Communication.Enums.ExpenseStatus.Pending
            })
        };
    }

    /// <summary>
    /// Installment 1 lands on the first competence month a due day allows; each later one advances
    /// exactly one month. EndMonth is set here, alongside DueDay and InstallmentCount, from
    /// installment N's own reference month - it is computed, never accepted from the request.
    /// </summary>
    private static List<DebtInstallment> BuildInstallments(RequestRegisterDebtJson request, Debt debt)
    {
        var dueDay = request.DueDay!.Value;
        var count = request.InstallmentCount!.Value;

        var firstCompetenceMonth = DebtScheduleBuilder.FirstCompetenceMonth(request.StartDate, dueDay);
        var amounts = InstallmentAmountCalculator.Split(request.TotalAmount, count);

        var installments = new List<DebtInstallment>(count);

        for (var number = 1; number <= count; number++)
        {
            var referenceMonth = firstCompetenceMonth.AddMonths(number - 1);

            installments.Add(new DebtInstallment
            {
                DebtId = debt.Id,
                Number = number,
                ReferenceMonth = referenceMonth,
                DueDate = DebtScheduleBuilder.DueDateIn(referenceMonth, dueDay),
                ExpectedAmount = amounts[number - 1]
            });
        }

        debt.DueDay = dueDay;
        debt.InstallmentCount = count;
        debt.EndMonth = installments[^1].ReferenceMonth;

        return installments;
    }

    private static void Validate(RequestRegisterDebtJson request)
    {
        var result = new RegisterDebtValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
