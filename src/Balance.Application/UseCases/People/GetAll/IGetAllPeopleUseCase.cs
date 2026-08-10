using Balance.Communication.Responses;

namespace Balance.Application.UseCases.People.GetAll;

public interface IGetAllPeopleUseCase
{
    Task<ResponsePeopleJson> Execute();
}
