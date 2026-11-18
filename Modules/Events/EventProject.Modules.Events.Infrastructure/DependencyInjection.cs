using EventProject.Modules.Events.Core.Application;
using EventProject.Modules.Events.Infrastructure.Persistence;
using EventProject.Modules.Events.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;

namespace EventProject.Modules.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleModelConfiguration, EventModelConfiguration>();
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}