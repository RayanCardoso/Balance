using AutoMapper;
using Balance.Communication.Requests;
using Balance.Communication.Responses;
using Balance.Domain.Entities;

namespace Balance.Application.AutoMapper;

public class AutoMapping : Profile
{
    public AutoMapping()
    {
        RequestToEntity();
        EntityToResponse();
    }

    private void RequestToEntity()
    {
        CreateMap<RequestRegisterUserJson, User>()
            .ForMember(dest => dest.Password, config => config.Ignore());

        CreateMap<RequestRegisterPersonJson, Person>();
    }

    private void EntityToResponse()
    {
        CreateMap<Person, ResponsePersonJson>();
    }
}
