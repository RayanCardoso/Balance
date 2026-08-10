using Balance.Application.AutoMapper;
using Balance.Application.UseCases.Login.DoLogin;
using Balance.Application.UseCases.People.GetAll;
using Balance.Application.UseCases.People.Register;
using Balance.Application.UseCases.Users.Register;
using Microsoft.Extensions.DependencyInjection;

namespace Balance.Application;

public static class DependencyInjectionExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);
        AddAutoMapper(services);
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapping>());
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IDoLoginUseCase, DoLoginUseCase>();
        services.AddScoped<IRegisterPersonUseCase, RegisterPersonUseCase>();
        services.AddScoped<IGetAllPeopleUseCase, GetAllPeopleUseCase>();
    }
}
