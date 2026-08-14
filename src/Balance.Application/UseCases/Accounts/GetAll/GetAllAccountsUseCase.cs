using Balance.Communication.Responses;
using Balance.Domain.Repositories.Accounts;
using Balance.Domain.Services.LoggedUser;

namespace Balance.Application.UseCases.Accounts.GetAll;

public class GetAllAccountsUseCase : IGetAllAccountsUseCase
{
    private readonly IAccountReadOnlyRepository _readOnlyRepository;
    private readonly ILoggedUser _loggedUser;

    public GetAllAccountsUseCase(IAccountReadOnlyRepository readOnlyRepository, ILoggedUser loggedUser)
    {
        _readOnlyRepository = readOnlyRepository;
        _loggedUser = loggedUser;
    }

    public async Task<ResponseAccountsJson> Execute()
    {
        var loggedUser = await _loggedUser.Get();

        var accounts = await _readOnlyRepository.GetAll(loggedUser);

        return new ResponseAccountsJson
        {
            Accounts = accounts.Select(account => new ResponseAccountJson
            {
                Id = account.Id,
                Name = account.Name,
                Institution = account.Institution,
                PersonId = account.PersonId,
                ClosingDay = account.ClosingDay,
                DueDay = account.DueDay,
                Limit = account.Limit
            }).ToList()
        };
    }
}
