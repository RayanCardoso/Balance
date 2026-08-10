using AutoMapper;
using Balance.Application.AutoMapper;
using Microsoft.Extensions.Logging;

namespace CommonTestUtilities.Mapper;

public class MapperBuilder
{
    public static IMapper Build()
    {
        var loggerFactory = new LoggerFactory();

        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new AutoMapping());
        }, loggerFactory);

        return configuration.CreateMapper();
    }
}
