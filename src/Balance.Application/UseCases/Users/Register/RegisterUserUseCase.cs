using AutoMapper;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;
using Balance.Domain.Repositories;
using Balance.Domain.Repositories.People;
using Balance.Domain.Repositories.Users;
using Balance.Domain.Security.Cryptography;
using Balance.Domain.Security.Tokens;
using Balance.Exception;
using Balance.Exception.ExceptionBase;
using FluentValidation.Results;

namespace Balance.Application.UseCases.Users.Register;

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserReadOnlyRepository _readOnlyRepository;
    private readonly IUserWriteOnlyRepository _writeOnlyRepository;
    private readonly IPersonWriteOnlyRepository _personWriteOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordEncripter _passwordEncripter;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public RegisterUserUseCase(
        IUserReadOnlyRepository readOnlyRepository,
        IUserWriteOnlyRepository writeOnlyRepository,
        IPersonWriteOnlyRepository personWriteOnlyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordEncripter passwordEncripter,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _readOnlyRepository = readOnlyRepository;
        _writeOnlyRepository = writeOnlyRepository;
        _personWriteOnlyRepository = personWriteOnlyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordEncripter = passwordEncripter;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<ResponseRegisteredUserJson> Execute(RequestRegisterUserJson request)
    {
        await Validate(request);

        var user = _mapper.Map<User>(request);
        user.Password = _passwordEncripter.Encrypt(request.Password);

        await _writeOnlyRepository.Add(user);

        await _personWriteOnlyRepository.Add(new Person
        {
            Name = user.Name,
            UserId = user.Id,
            IsAccountOwner = true
        });

        await _unitOfWork.Commit();

        return new ResponseRegisteredUserJson
        {
            Name = user.Name,
            Token = _accessTokenGenerator.Generate(user)
        };
    }

    private async Task Validate(RequestRegisterUserJson request)
    {
        var result = new RegisterUserValidator().Validate(request);

        var emailAlreadyExists = await _readOnlyRepository.ExistActiveUserWithEmail(request.Email);
        if (emailAlreadyExists)
        {
            result.Errors.Add(new ValidationFailure(
                nameof(request.Email), ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
        }

        if (result.IsValid == false)
        {
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}
