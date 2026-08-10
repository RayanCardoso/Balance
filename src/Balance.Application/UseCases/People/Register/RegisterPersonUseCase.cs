using AutoMapper;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;
using Balance.Exception.ExceptionBase;

namespace Balance.Application.UseCases.People.Register;

public class RegisterPersonUseCase : IRegisterPersonUseCase
{
    private readonly IPersonWriteOnlyRepository _writeOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public RegisterPersonUseCase(
        IPersonWriteOnlyRepository writeOnlyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _writeOnlyRepository = writeOnlyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<ResponsePersonJson> Execute(RequestRegisterPersonJson request)
    {
        Validate(request);

        var loggedUser = await _loggedUser.Get();

        var person = _mapper.Map<Person>(request);
        person.UserId = loggedUser.Id;
        person.IsAccountOwner = false;

        await _writeOnlyRepository.Add(person);
        await _unitOfWork.Commit();

        return _mapper.Map<ResponsePersonJson>(person);
    }

    private static void Validate(RequestRegisterPersonJson request)
    {
        var result = new RegisterPersonValidator().Validate(request);

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
