using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Abstractions.Messaging;

namespace Mms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        RegisterHandlers(services, typeof(ICommandHandler<>));
        RegisterHandlers(services, typeof(ICommandHandler<,>));
        RegisterHandlers(services, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Type handlerInterfaceType)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType));

        foreach (var handlerType in handlerTypes)
        {
            var implementedInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

            foreach (var interfaceType in implementedInterfaces)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }
    }
}
