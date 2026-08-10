using AutoMapper;
using Balance.Communication.Responses;
using Balance.Domain.Repositories.People;
using Balance.Domain.Services.LoggedUser;

namespace Balance.Application.UseCases.People.GetAll;

public class GetAllPeopleUseCase : IGetAllPeopleUseCase
{
    private readonly IPersonReadOnlyRepository _readOnlyRepository;
    private readonly IMapper _mapper;
    private readonly ILoggedUser _loggedUser;

    public GetAllPeopleUseCase(
        IPersonReadOnlyRepository readOnlyRepository,
        IMapper mapper,
        ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _mapper = mapper;
        _loggedUser = loggedUser;
    }

    public async Task<ResponsePeopleJson> Execute()
    {
        var loggedUser = await _loggedUser.Get();

        var people = await _readOnlyRepository.GetAll(loggedUser);

        return new ResponsePeopleJson
        {
            People = _mapper.Map<List<ResponsePersonJson>>(people)
        };
    }
}
