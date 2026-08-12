using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Accounts.Register;

public class RegisterAccountUseCase : IRegisterAccountUseCase
{
    private readonly IAccountWriteOnlyRepository _writeOnlyRepository;
    private readonly IPersonReadOnlyRepository _personReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterAccountUseCase(
        IAccountWriteOnlyRepository writeOnlyRepository,
        IPersonReadOnlyRepository personReadOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _personReadOnlyRepository = personReadOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseAccountJson> Execute(RequestRegisterAccountJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = await _personReadOnlyRepository.GetById(loggedUser, request.PersonId)
            ?? throw new NotFoundException(ResourceErrorMessages.PERSON_NOT_FOUND);

        var account = new Account
        {
            Name = request.Name,
            Institution = request.Institution,
            ClosingDay = request.ClosingDay,
            DueDay = request.DueDay,
            Limit = request.Limit,
            PersonId = person.Id
        };

        await _writeOnlyRepository.Add(account);
        await _unitOfWork.Commit();

        return new ResponseAccountJson
        {
            Id = account.Id,
            Name = account.Name,
            Institution = account.Institution,
            PersonId = account.PersonId,
            ClosingDay = account.ClosingDay,
            DueDay = account.DueDay,
            Limit = account.Limit
        };
    }

    private static void Validate(RequestRegisterAccountJson request)
    {
        var result = new RegisterAccountValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
