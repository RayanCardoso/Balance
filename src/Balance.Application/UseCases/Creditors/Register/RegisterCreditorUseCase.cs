using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.Creditors.Register;

public class RegisterCreditorUseCase : IRegisterCreditorUseCase
{
    private readonly ICreditorWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;

    public RegisterCreditorUseCase(
        ICreditorWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseCreditorJson> Execute(RequestRegisterCreditorJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var creditor = new Creditor
        {
            Name = request.Name,
            Type = (Balance.Domain.Enums.CreditorType)request.Type,
            Contact = request.Contact,
            Notes = request.Notes,
            UserId = loggedUser.Id
        };

        await _writeOnlyRepository.Add(creditor);
        await _unitOfWork.Commit();

        return new ResponseCreditorJson
        {
            Id = creditor.Id,
            Name = creditor.Name,
            Type = (Balance.Communication.Enums.CreditorType)creditor.Type,
            Contact = creditor.Contact,
            Notes = creditor.Notes,
            Archived = creditor.Archived
        };
    }

    private static void Validate(RequestRegisterCreditorJson request)
    {
        var result = new RegisterCreditorValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
