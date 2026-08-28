using Balance.Communication.Responses;
using Balance.Domain.Repositories.Creditors;
using Balance.Domain.Services.LoggedUser;

namespace Balance.Application.UseCases.Creditors.GetAll;

public class GetAllCreditorsUseCase : IGetAllCreditorsUseCase
{
    private readonly ICreditorReadOnlyRepository _readOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetAllCreditorsUseCase(ICreditorReadOnlyRepository readOnlyRepository, ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseCreditorsJson> Execute(bool includeArchived)
    {
        var loggedUser = await _loggedUser.Get();

        var creditors = await _readOnlyRepository.GetAll(loggedUser, includeArchived);

        return new ResponseCreditorsJson
        {
            Creditors = creditors.Select(creditor => new ResponseCreditorJson
            {
                Id = creditor.Id,
                Name = creditor.Name,
                Type = (Balance.Communication.Enums.CreditorType)creditor.Type,
                Contact = creditor.Contact,
                Notes = creditor.Notes,
                Archived = creditor.Archived
            }).ToList()
        };
    }
}
